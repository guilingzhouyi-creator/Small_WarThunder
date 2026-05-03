#!/usr/bin/env python3
"""
自动化仓管 Agent：解析《东七三》开发日志格式 commit，更新 DEVLOG.md 和 CHANGELOG.md。

触发：GitHub Actions push 事件 → 本脚本遍历所有新 commit → 生成/追加日志。
"""

import os
import re
import sys
import subprocess
from datetime import datetime


# ========== 配置 ==========
DEVLOG_PATH = "DEVLOG.md"
CHANGELOG_PATH = "CHANGELOG.md"

# DEVLOG 插入位置标记：在此行之后追加新条目
DEVLOG_INSERT_MARKER = "按时间倒序记录每次提交的开发变更。\n"
DEVLOG_ENTRY_TEMPLATE = """### {timestamp} {title}

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


# ========== 工具函数 ==========
def get_new_commits(prev_commit, current_commit):
    """获取两个提交之间的所有新 commit（含 current_commit 本身）"""
    cmd = [
        "git", "log",
        f"{prev_commit}..{current_commit}",
        "--pretty=format:%H%n%B%n---END---",
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
    blocks = raw.split("---END---\n")
    for block in blocks:
        block = block.strip()
        if not block:
            continue
        lines = block.split("\n")
        commit_hash = lines[0].strip()
        commit_msg = "\n".join(lines[1:]).strip()
        commits.append((commit_hash, commit_msg))
    return commits


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

    # 第1行时间戳用作标题日期
    parsed["title"] = parsed["timestamp"]

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
            title=entry["title"],
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
    """提交对 DEVLOG.md 和 CHANGELOG.md 的修改并推送"""
    has_changes_devlog = subprocess.run(
        ["git", "diff", "--quiet", DEVLOG_PATH]
    ).returncode != 0
    has_changes_changelog = subprocess.run(
        ["git", "diff", "--quiet", CHANGELOG_PATH]
    ).returncode != 0

    if not has_changes_devlog and not has_changes_changelog:
        print("[INFO] 文件无变化，无需提交")
        return

    subprocess.run(["git", "config", "user.name", "DevLog Auto Agent"])
    subprocess.run(["git", "config", "user.email", "agent@small-warthunder.dev"])

    subprocess.run(["git", "add", DEVLOG_PATH, CHANGELOG_PATH])
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
    prev_commit = os.environ.get("GITHUB_EVENT_BEFORE")
    current_commit = os.environ.get("GITHUB_EVENT_AFTER")

    if not prev_commit or not current_commit:
        print("[ERROR] 缺少 GITHUB_EVENT_BEFORE / GITHUB_EVENT_AFTER 环境变量")
        # 尝试从 push event payload 获取
        event_path = os.environ.get("GITHUB_EVENT_PATH")
        if event_path and os.path.exists(event_path):
            import json
            with open(event_path, "r") as f:
                event = json.load(f)
            prev_commit = event.get("before", "")
            current_commit = event.get("after", "")

    if not prev_commit or not current_commit:
        print("[ERROR] 无法获取提交范围，退出")
        sys.exit(1)

    if prev_commit == "0000000000000000000000000000000000000000":
        print("[INFO] 首次推送，跳过")
        sys.exit(0)

    print(f"[INFO] 扫描提交范围：{prev_commit[:8]}..{current_commit[:8]}")

    commits = get_new_commits(prev_commit, current_commit)
    if not commits:
        print("[INFO] 没有新提交可处理")
        sys.exit(0)

    print(f"[INFO] 发现 {len(commits)} 个新提交")

    # 解析并分类
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

    if not devlog_entries:
        print("[INFO] 没有符合格式的提交")
        sys.exit(0)

    # 更新文件
    devlog_updated = update_devlog(devlog_entries)
    changelog_updated = update_changelog(major_entries)

    if devlog_updated or changelog_updated:
        commit_and_push()

    print("[DONE] 自动化仓管任务完成")


if __name__ == "__main__":
    main()
