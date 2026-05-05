#!/usr/bin/env python3
"""
自动化仓管 Agent：解析《东七三》开发日志格式 commit，更新 DEVLOG.md 和 CHANGELOG.md。

触发：GitHub Actions push 事件 → 本脚本遍历所有新 commit → 生成/追加日志。

逻辑：
  1. 读取 .last_processed 状态文件，确定上次处理到的 commit
  2. 扫描 .last_processed..HEAD 范围的全部 commit
  3. 检查是否有 ⚡重大改动 commit：
     - 有 → 处理范围内全部 commit → 写入 DEVLOG + CHANGELOG → 更新状态 → commit & push
     - 无 → 静默退出（不处理，等下次重大改动时补上）
"""

import os
import re
import sys
import subprocess
from datetime import datetime


# ========== 配置 ==========
DEVLOG_PATH = "DEVLOG.md"
CHANGELOG_PATH = "CHANGELOG.md"
STATE_FILE = ".github/scripts/.last_processed"  # 持久化状态文件：上次处理到的 commit hash

# DEVLOG 插入位置标记：在此行之后追加新条目
DEVLOG_INSERT_MARKER = "<!-- DEVLOG_ENTRIES_START -->\n"
# 标题使用 summary（从新增内容第一项提取的简短摘要），避免"时间戳 — 时间戳" 纯日期重复
DEVLOG_ENTRY_TEMPLATE = """### {timestamp} — {summary}

**新增内容：**
{additions}

**改动及优化描述：**
{changes}

"""

# CHANGELOG 版本号 pattern（从现有文件中提取最新版本）
CHANGELOG_VERSION_RE = re.compile(r'## \[(v(\d+)\.(\d+)\.(\d+)-beta)\]')
# 重大改动行 pattern
MAJOR_CHANGE_RE = re.compile(r'^⚡重大改动：(.+)$')
# commit 第1行时间戳提取
TIMESTAMP_RE = re.compile(r'^《东七三》开发日志<(.+)>$')
# 新增内容行
ADDITIONS_RE = re.compile(r'^新增内容：(.*)$')
# 改动及优化描述行
CHANGES_RE = re.compile(r'^改动及优化描述：(.*)$')

# git log 分隔符：使用独特标记避免与提交内容冲突
COMMIT_SEPARATOR = "<<<COMMIT_SEPARATOR>>>"


# ========== 状态管理 ==========
def get_last_processed():
    """读取持久化状态文件，获取上次处理到的 commit hash。不存在则返回 None"""
    if os.path.exists(STATE_FILE):
        with open(STATE_FILE, "r") as f:
            commit_hash = f.read().strip()
            if commit_hash:
                print(f"[STATE] 上次处理到: {commit_hash[:8]}")
                return commit_hash
    print("[STATE] 无状态文件，将从头开始扫描")
    return None


def save_last_processed(commit_hash):
    """将当前处理到的 commit hash 写入状态文件"""
    os.makedirs(os.path.dirname(STATE_FILE), exist_ok=True)
    with open(STATE_FILE, "w") as f:
        f.write(commit_hash + "\n")
    print(f"[STATE] 已更新状态: {commit_hash[:8]}")


def get_head_commit():
    """获取当前 HEAD commit hash"""
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        capture_output=True, text=True
    )
    if result.returncode != 0:
        print(f"[ERROR] git rev-parse HEAD failed: {result.stderr}")
        sys.exit(1)
    return result.stdout.strip()


# ========== 工具函数 ==========
def get_new_commits(prev_commit, current_commit):
    """获取两个提交之间的所有新 commit（含 current_commit 本身）"""
    cmd = [
        "git", "log",
        f"{prev_commit}..{current_commit}",
        f"--pretty=format:%H%n%B%n{COMMIT_SEPARATOR}",
        "--reverse"
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"[ERROR] git log failed: {result.stderr}")
        return []

    raw = result.stdout.strip()
    if not raw:
        return []

    commits = []
    blocks = raw.split(f"{COMMIT_SEPARATOR}\n")
    for block in blocks:
        block = block.strip()
        if not block:
            continue
        # 跳过可能由分隔符引入的空行
        lines = [l for l in block.split("\n") if l.strip() != COMMIT_SEPARATOR]
        if not lines:
            continue
        commit_hash = lines[0].strip()
        commit_msg = "\n".join(lines[1:]).strip()
        commits.append((commit_hash, commit_msg))
    return commits


def extract_summary(additions_text):
    """从新增内容第一项提取简短摘要（≤50字符），避免 DEVLOG 标题出现纯日期重复"""
    first_item = additions_text.split("、")[0].split("\n")[0].strip()
    if not first_item or first_item == "（无）":
        return "更新"
    # 去掉常见的引号/标记前缀
    first_item = first_item.lstrip("- ").lstrip("`").rstrip("`")
    if len(first_item) > 50:
        first_item = first_item[:47] + "..."
    return first_item


def parse_commit_message(commit_hash, msg):
    """解析 commit message，返回 dict 或 None（格式不符则跳过）"""
    # 跳过 # 注释行和空行
    lines = [l for l in msg.split("\n") if l.strip() and not l.strip().startswith("#")]

    if len(lines) < 3:
        return None

    parsed = {"hash": commit_hash[:8]}

    # 第1行：时间戳
    m = TIMESTAMP_RE.match(lines[0])
    if not m:
        return None
    parsed["timestamp"] = m.group(1).strip()

    # 第2行：新增内容
    m = ADDITIONS_RE.match(lines[1])
    if not m:
        return None
    parsed["additions"] = m.group(1).strip() or "（无）"

    # 第3行：改动及优化描述
    m = CHANGES_RE.match(lines[2])
    if not m:
        return None
    parsed["changes"] = m.group(1).strip() or "（无）"

    # 第4行（可选）：重大改动标记
    if len(lines) >= 4:
        m = MAJOR_CHANGE_RE.match(lines[3])
        if m:
            parsed["major_change"] = m.group(1).strip()

    # 摘要：从新增内容第一项提取
    parsed["summary"] = extract_summary(parsed["additions"])

    return parsed


def update_devlog(entries):
    """追加新条目到 DEVLOG.md"""
    if not entries:
        return False

    with open(DEVLOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 在插入标记后按倒序插入（遍历 entries 倒序产出）
    new_entries_text = ""
    for entry in reversed(entries):
        new_entries_text += DEVLOG_ENTRY_TEMPLATE.format(
            timestamp=entry["timestamp"],
            summary=entry.get("summary", entry.get("title", "更新")),
            additions=entry["additions"],
            changes=entry["changes"]
        )
        new_entries_text += "---\n\n"

    marker_index = content.find(DEVLOG_INSERT_MARKER)
    if marker_index == -1:
        print("[WARN] DEVLOG_INSERT_MARKER 未找到，追加到文件末尾")
        content = content.rstrip() + "\n\n" + new_entries_text
    else:
        insert_pos = marker_index + len(DEVLOG_INSERT_MARKER)
        content = content[:insert_pos] + "\n" + new_entries_text + content[insert_pos:]

    with open(DEVLOG_PATH, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"[OK] DEVLOG.md 已更新（追加 {len(entries)} 条记录）")
    return True


def get_latest_version():
    """从 CHANGELOG.md 提取最新版本号"""
    with open(CHANGELOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 找第一个 [vX.Y.Z-beta]
    m = CHANGELOG_VERSION_RE.search(content)
    if m:
        major = int(m.group(2))
        minor = int(m.group(3))
        patch = int(m.group(4))
        return (major, minor, patch), m.group(1)

    # 兜底：从 v0.0.000-beta 开始
    return (0, 0, 0), "v0.0.000-beta"


def bump_version(version_tuple):
    """递增版本号中间位 (minor)，reset patch"""
    major, minor, patch = version_tuple
    return (major, minor + 1, 0), f"v{major}.{minor + 1}.000-beta"


def parse_manual_version(version_str, current_version_tuple):
    """尝试从重大改动行中解析手动版本号，失败则自动递增"""
    manual_re = re.compile(r'v(\d+)\.(\d+)\.(\d+)-(\w+)')
    m = manual_re.search(version_str)
    if m:
        return (int(m.group(1)), int(m.group(2)), int(m.group(3))), m.group(0)
    # 否则自动递增
    return bump_version(current_version_tuple)


def update_changelog(major_entries):
    """为所有重大改动条目更新 CHANGELOG.md"""
    if not major_entries:
        return False

    current_version_tuple, current_version_str = get_latest_version()

    with open(CHANGELOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 找到第一个 ## [vX.Y.Z-beta] 的位置，在此前插入
    first_version_match = CHANGELOG_VERSION_RE.search(content)

    for entry in major_entries:
        version_tuple, version_str = parse_manual_version(
            entry.get("major_change", ""), current_version_tuple
        )

        # 构建新版本条目
        today = datetime.now().strftime("%Y-%m-%d")
        new_entry = f"""## [{version_str}](https://github.com/{get_repo_name()}/releases/tag/{version_str}) — {today}

### ⚡ 重大改动 — {entry['timestamp']}

**新增内容：**
{entry['additions']}

**改动及优化描述：**
{entry['changes']}

"""
        # 插入到第一个版本条目之前
        if first_version_match:
            insert_pos = first_version_match.start()
            content = content[:insert_pos] + new_entry + content[insert_pos:]
        else:
            # 没有现有版本条目，追加到末尾
            content = content.rstrip() + "\n\n" + new_entry

        current_version_tuple = version_tuple  # 多次重大改动时避免版本号冲突

    with open(CHANGELOG_PATH, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"[OK] CHANGELOG.md 已更新（追加 {len(major_entries)} 个版本条目）")
    return True


def get_repo_name():
    """获取当前仓库名，如 guilingzhouyi-creator/Small_WarThunder"""
    repo = os.environ.get("GITHUB_REPOSITORY", "guilingzhouyi-creator/Small_WarThunder")
    return repo


def commit_and_push():
    """提交对 DEVLOG.md、CHANGELOG.md 和状态文件的修改并推送"""
    has_changes_devlog = subprocess.run(
        ["git", "diff", "--quiet", DEVLOG_PATH]
    ).returncode != 0
    has_changes_changelog = subprocess.run(
        ["git", "diff", "--quiet", CHANGELOG_PATH]
    ).returncode != 0
    has_changes_state = subprocess.run(
        ["git", "diff", "--quiet", STATE_FILE]
    ).returncode != 0

    if not has_changes_devlog and not has_changes_changelog and not has_changes_state:
        print("[INFO] 文件无变化，无需提交")
        return

    subprocess.run(["git", "config", "user.name", "DevLog Auto Agent"])
    subprocess.run(["git", "config", "user.email", "agent@small-warthunder.dev"])

    subprocess.run(["git", "add", DEVLOG_PATH, CHANGELOG_PATH, STATE_FILE])
    subprocess.run([
        "git", "commit",
        "-m", f"《东七三》开发日志<{datetime.now().strftime('%Y-%m-%d %H:%M')}>",
        "-m", "新增内容：DEVLOG / CHANGELOG 自动更新",
        "-m", "改动及优化描述：自动化仓管 Agent 根据提交生成日志"
    ])
    subprocess.run(["git", "push"])
    print("[OK] 日志变更已提交并推送")


# ========== 主流程 ==========
def main():
    # ── 1. 确定扫描范围 ──
    # 从状态文件读取"上次处理到"的 commit，没有则用 push 前 HEAD
    last_processed = get_last_processed()

    if last_processed is None:
        # 首次运行：用 push event 的 before 作为起点
        prev_commit = os.environ.get("GITHUB_EVENT_BEFORE")
        event_path = os.environ.get("GITHUB_EVENT_PATH")
        if (not prev_commit or prev_commit == "0000000000000000000000000000000000000000") and event_path:
            import json
            if os.path.exists(event_path):
                with open(event_path, "r") as f:
                    event = json.load(f)
                prev_commit = event.get("before", "")

        if not prev_commit or prev_commit == "0000000000000000000000000000000000000000":
            print("[INFO] 首次推送或无法确定起点，使用 HEAD~1 作为起点")
            result = subprocess.run(
                ["git", "rev-parse", "HEAD~1"],
                capture_output=True, text=True
            )
            if result.returncode == 0:
                prev_commit = result.stdout.strip()
            else:
                print("[ERROR] 无法确定扫描起点，退出")
                sys.exit(1)
    else:
        prev_commit = last_processed

    head_commit = get_head_commit()

    if prev_commit == head_commit:
        print("[INFO] 没有新提交（.last_processed == HEAD），退出")
        sys.exit(0)

    print(f"[INFO] 扫描提交范围：{prev_commit[:8]}..{head_commit[:8]}")

    commits = get_new_commits(prev_commit, head_commit)
    if not commits:
        print("[INFO] 没有新提交可处理")
        sys.exit(0)

    print(f"[INFO] 发现 {len(commits)} 个新提交")

    # ── 2. 解析并分类 ──
    devlog_entries = []
    major_entries = []

    for commit_hash, msg in commits:
        parsed = parse_commit_message(commit_hash, msg)
        if not parsed:
            print(f"[SKIP] {commit_hash[:8]} — 格式不符，跳过")
            continue

        devlog_entries.append(parsed)

        if "major_change" in parsed:
            major_entries.append(parsed)
            print(f"[MAJOR] {commit_hash[:8]} — ⚡重大改动：{parsed['major_change']}")

        print(f"[OK] {commit_hash[:8]} — {parsed['timestamp']}")

    # ── 3. 决策：是否有重大改动？ ──
    if not devlog_entries:
        print("[INFO] 没有符合格式的提交")
        sys.exit(0)

    if not major_entries:
        # 没有重大改动 → 不处理，不更新状态文件，等下次
        print(f"[INFO] 没有重大改动，跳过日志自动提交")
        print(f"[INFO] ({len(devlog_entries)} 条普通提交将在下次重大改动时批量写入)")
        sys.exit(0)

    # ── 4. 处理：更新 DEVLOG + CHANGELOG → 更新状态 → 提交推送 ──
    print(f"[ACTION] 检测到 {len(major_entries)} 条重大改动，开始处理范围内全部 {len(devlog_entries)} 条提交")

    devlog_updated = update_devlog(devlog_entries)
    changelog_updated = update_changelog(major_entries)

    # 更新持久化状态到 HEAD
    save_last_processed(head_commit)

    if devlog_updated or changelog_updated:
        commit_and_push()

    print(f"[DONE] 自动化仓管任务完成")
    print(f"  - 写入 DEVLOG：{len(devlog_entries)} 条")
    print(f"  - 写入 CHANGELOG：{len(major_entries)} 条版本")
    print(f"  - 状态已更新到：{head_commit[:8]}")


if __name__ == "__main__":
    main()
