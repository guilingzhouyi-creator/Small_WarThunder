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
        "--pretty=format:%H%n%ai%n%B%n" + COMMIT_SEPARATOR,
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
        if len(lines) < 2:
            continue
        commit_hash = lines[0].strip()
        commit_date = lines[1].strip()  # ISO 8601: 2026-05-17 15:30:00 +0800
        commit_msg = "\n".join(lines[2:]).strip()
        commits.append((commit_hash, commit_date, commit_msg))
    return commits


def normalize_timestamp(raw):
    """统一时间戳为 YY.M.D 格式，兼容全日期/简写/斜杠/点分隔，月日不带前导零"""
    raw = raw.strip()
    # 已符合 YY.M.D HH:MM 或 YY.M.D → 截取日期部分
    m = re.match(r'^(\d{2})\.(\d{1,2})\.(\d{1,2})(?:\s+\d{2}:\d{2})?$', raw)
    if m:
        return f"{int(m.group(1))}.{int(m.group(2))}.{int(m.group(3))}"

    # 全日期 YYYY-MM-DD HH:MM → 取后两位，转点分隔
    m = re.match(r'^\d{2}(\d{2})-(\d{2})-(\d{2})(?:\s+\d{2}:\d{2})?$', raw)
    if m:
        return f"{int(m.group(1))}.{int(m.group(2))}.{int(m.group(3))}"

    # YY-MM-DD HH:MM 或 YY-MM-DD → 转点分隔
    m = re.match(r'^(\d{2})-(\d{2})-(\d{2})(?:\s+\d{2}:\d{2})?$', raw)
    if m:
        return f"{int(m.group(1))}.{int(m.group(2))}.{int(m.group(3))}"

    # 斜杠分隔 YY/M/D
    m = re.match(r'^(\d{2})/(\d{1,2})/(\d{1,2})$', raw)
    if m:
        return f"{int(m.group(1))}.{int(m.group(2))}.{int(m.group(3))}"

    # 点分隔 YY.MM.DD (带前导零) → 去除前导零
    m = re.match(r'^(\d{2})\.(\d{2})\.(\d{2})$', raw)
    if m:
        return f"{int(m.group(1))}.{int(m.group(2))}.{int(m.group(3))}"

    return raw


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


def parse_devlog_entries():
    """解析 DEVLOG.md 中所有历史条目，返回 [{timestamp, summary, additions, changes}, ...]"""
    if not os.path.exists(DEVLOG_PATH):
        return []

    with open(DEVLOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    entries = []
    # 匹配每个 ### 条目块
    entry_re = re.compile(
        r'###\s+(\S+)\s+—\s+(.+?)\n\n'
        r'\*\*新增内容：\*\*\s*\n(.+?)\n\n'
        r'\*\*改动及优化描述：\*\*\s*\n(.+?)\n\n'
        r'---',
        re.DOTALL
    )
    for m in entry_re.finditer(content):
        entries.append({
            "timestamp": m.group(1).strip(),
            "summary": m.group(2).strip(),
            "additions": m.group(3).strip(),
            "changes": m.group(4).strip()
        })
    print(f"[DEVLOG PARSE] 解析到 {len(entries)} 条历史条目")
    return entries


def rebuild_devlog_content(entries):
    """用修正后的条目列表重建 DEVLOG.md 完整内容（保留头尾非条目部分）"""
    if not os.path.exists(DEVLOG_PATH):
        print("[DEVLOG REBUILD] DEVLOG.md 不存在，跳过")
        return

    with open(DEVLOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 取文件头（第一个 ### 之前的部分）
    first_entry_idx = content.find("### ")
    if first_entry_idx == -1:
        print("[DEVLOG REBUILD] 未找到条目标记，跳过")
        return
    header = content[:first_entry_idx]

    # 取文件尾（最后一个 --- 之后的部分，如许可声明等）
    last_sep = content.rfind("\n---")
    if last_sep == -1:
        footer = ""
    else:
        footer = content[last_sep + 4:]  # 跳过 ---

    # 重建条目区
    new_entries_text = ""
    for entry in entries:
        ts = normalize_timestamp(entry.get("timestamp", ""))
        summary = entry.get("summary", entry.get("title", "更新"))
        additions = entry.get("additions", "")
        changes = entry.get("changes", "")
        new_entries_text += DEVLOG_ENTRY_TEMPLATE.format(
            timestamp=ts,
            summary=summary,
            additions=additions,
            changes=changes
        )
        new_entries_text += "---\n\n"

    new_content = header.rstrip() + "\n\n" + new_entries_text.rstrip()
    if footer.strip():
        new_content += "\n\n" + footer.strip()

    with open(DEVLOG_PATH, "w", encoding="utf-8") as f:
        f.write(new_content)
    print(f"[DEVLOG REBUILD] 已重建 DEVLOG.md（{len(entries)} 条条目）")


def self_review_devlog():
    """
    调用 Deepseek API 对 DEVLOG.md 历史条目进行去重、归并和标题修正。
    纯追加导致的重复/近似条目会被合并，空泛标题会被改写为有意义的总结。
    """
    entries = parse_devlog_entries()
    if len(entries) < 3:
        print("[REVIEW] 条目数不足3条，跳过自检")
        return

    api_key = os.environ.get("DEEPSEEK_API_KEY", "").strip()
    if not api_key:
        print("[REVIEW] 未设置 DEEPSEEK_API_KEY，跳过自检")
        return

    # 构建 prompt：列出所有条目的时间戳+标题
    lines = [f"{i+1}. [{e['timestamp']}] {e['summary']}: 新增={e['additions'][:60]} | 改动={e['changes'][:60]}"
             for i, e in enumerate(entries)]
    list_text = "\n".join(lines)

    prompt = (
        "以下是游戏开发日志 DEVLOG 的历史条目列表。请检查是否存在以下问题：\n"
        "1. 相邻条目描述同一功能反复修改，需要合并为一条\n"
        "2. 条目标题为纯日期或「更新」等空泛文本，需要根据内容改写\n"
        "3. 条目时间戳格式不统一\n\n"
        f"{list_text}\n\n"
        "请返回 JSON 格式的修改指令列表，每条指令包含：\n"
        "- action: merge / rewrite / delete / keep\n"
        "- indices: 涉及的条目序号列表 (1-based)\n"
        "- new_summary: 合并/改写后的标题（rewrite/merge 时需要）\n"
        "- reason: 修改原因（一句话）\n\n"
        '仅返回 JSON 数组，不要额外解释。'
    )

    api_url = os.environ.get("DEEPSEEK_API_URL", "https://api.deepseek.com")
    chat_url = f"{api_url.rstrip('/')}/v1/chat/completions"
    model = os.environ.get("DEEPSEEK_MODEL", "deepseek-v4-flash")

    import json
    payload = {
        "model": model,
        "messages": [
            {"role": "system", "content": "你是一个开发日志审查助手。输出必须是严格的 JSON 数组。"},
            {"role": "user", "content": prompt}
        ],
        "max_tokens": 800,
        "temperature": 0.3
    }

    try:
        result = subprocess.run(
            [
                "curl", "-s", "-S",
                "-H", f"Authorization: Bearer {api_key}",
                "-H", "Content-Type: application/json",
                "-d", json.dumps(payload, ensure_ascii=False),
                "--connect-timeout", "10",
                "--max-time", "20",
                chat_url
            ],
            capture_output=True, text=True, timeout=25
        )

        if result.returncode != 0:
            print(f"[REVIEW] curl 失败: {result.stderr[:100]}")
            return

        response = json.loads(result.stdout)
        ai_text = response.get("choices", [{}])[0].get("message", {}).get("content", "").strip()

        if not ai_text:
            print("[REVIEW] AI 返回空响应")
            return

        # 尝试提取 JSON 数组
        json_start = ai_text.find("[")
        json_end = ai_text.rfind("]") + 1
        if json_start == -1 or json_end == 0:
            print(f"[REVIEW] 无法解析 AI 响应: {ai_text[:100]}")
            return

        instructions = json.loads(ai_text[json_start:json_end])

    except (json.JSONDecodeError, subprocess.TimeoutExpired) as e:
        print(f"[REVIEW] 解析/调用失败: {e}")
        return
    except Exception as e:
        print(f"[REVIEW] 未知错误: {e}")
        return

    # 应用指令
    merged = list(entries)
    modified = False

    for inst in sorted(instructions, key=lambda x: x.get("indices", [0])[0] if x.get("indices") else 0):
        indices = [i - 1 for i in inst.get("indices", [])]  # 转 0-based
        action = inst.get("action", "keep")

        if action == "merge" and len(indices) >= 2:
            merged_entry = {
                "timestamp": normalize_timestamp(merged[indices[0]]["timestamp"]),
                "summary": inst.get("new_summary", merged[indices[0]]["summary"]),
                "additions": "、".join(set(
                    item.strip()
                    for i in indices
                    for item in merged[i].get("additions", "").split("、")
                    if i < len(merged)
                )),
                "changes": "; ".join(set(
                    merged[i].get("changes", "").strip()
                    for i in indices
                    if i < len(merged)
                ))
            }
            merged[indices[0]] = merged_entry
            for i in sorted(indices[1:], reverse=True):
                merged.pop(i)
            print(f"[REVIEW] 合并条目 {[i+1 for i in indices]}: {inst.get('reason', '重复')}")
            modified = True

        elif action == "rewrite" and indices:
            i = indices[0]
            if i < len(merged):
                merged[i]["summary"] = inst.get("new_summary", merged[i]["summary"])
                print(f"[REVIEW] 改写条目 {i+1}: {inst.get('reason', '标题空泛')}")
                modified = True

        elif action == "delete" and indices:
            i = indices[0]
            if i < len(merged):
                print(f"[REVIEW] 删除条目 {i+1}: {inst.get('reason', '')}")
                merged.pop(i)
                modified = True

    if modified:
        rebuild_devlog_content(merged)
        print(f"[REVIEW] 自检完成，条目从 {len(entries)} → {len(merged)}")
    else:
        print("[REVIEW] 无需修改")


def parse_commit_message(commit_hash, msg, commit_date=None):
    """解析 commit message，返回 dict 或 None（格式不符则跳过）"""
    # 跳过仓管自身的提交
    if "⚡内部操作：" in msg:
        print(f"[SKIP] {commit_hash[:8]} — 仓管自动提交，跳过")
        return None

    # 跳过 # 注释行和空行
    lines = [l for l in msg.split("\n") if l.strip() and not l.strip().startswith("#")]

    if len(lines) < 3:
        return None

    parsed = {"hash": commit_hash[:8]}

    # 第1行：时间戳；为空时用 commit author date 回退
    m = TIMESTAMP_RE.match(lines[0])
    if m:
        parsed["timestamp"] = normalize_timestamp(m.group(1).strip())
    elif lines[0].startswith("《东七三》开发日志<") and commit_date:
        parsed["timestamp"] = normalize_timestamp(commit_date)
    else:
        return None

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
    """递增版本号最后位 (patch)，major.minor 由人工在 commit 中指定"""
    major, minor, patch = version_tuple
    patch += 1
    patch_str = f"{patch:03d}"
    return (major, minor, patch), f"v{major}.{minor}.{patch_str}-beta"


def summarize_with_deepseek(entries):
    """
    调用 Deepseek V4 Flash API 生成 CHANGELOG 发布说明。
    传入 entries 是本次重大改动相关的 commit 解析列表，
    返回一段简短总结（中文，1-2句话）。失败返回空字符串，由调用方回退。
    """
    api_key = os.environ.get("DEEPSEEK_API_KEY", "").strip()
    if not api_key:
        print("[AI] 未设置 DEEPSEEK_API_KEY，跳过 AI 总结")
        return ""

    # 构建 prompt 内容：合并所有重大改动条目的关键信息
    commits_text = ""
    for entry in entries:
        title = entry.get("major_change", entry.get("summary", ""))
        additions = entry.get("additions", "")
        changes = entry.get("changes", "")
        commits_text += f"- 标题：{title}\\n  新增：{additions}\\n  改动：{changes}\\n\\n"

    if not commits_text.strip():
        print("[AI] 没有有效内容可总结")
        return ""

    prompt = (
        "以下是本次版本涉及的开发日志内容，请将其润色为正式发布说明。\\n\\n"
        f"{commits_text}"
    )

    # 用 curl 调用 Deepseek API，零 Python 依赖
    api_url = os.environ.get("DEEPSEEK_API_URL", "https://api.deepseek.com")
    chat_url = f"{api_url.rstrip('/')}/v1/chat/completions"
    model = os.environ.get("DEEPSEEK_MODEL", "deepseek-v4-flash")

    payload = {
        "model": model,
        "messages": [
            {"role": "system", "content": "你是一个游戏版本发布编辑。请用正式、专业的发布说明语气，将以下开发日志内容润色为面向玩家的版本更新公告。语言简洁有力，不罗列条目，用自然语言概括核心变化。"},
            {"role": "user", "content": prompt}
        ],
        "max_tokens": 200,
        "temperature": 0.7
    }

    try:
        import json
        result = subprocess.run(
            [
                "curl", "-s", "-S",
                "-H", f"Authorization: Bearer {api_key}",
                "-H", "Content-Type: application/json",
                "-d", json.dumps(payload, ensure_ascii=False),
                "--connect-timeout", "10",
                "--max-time", "15",
                chat_url
            ],
            capture_output=True, text=True, timeout=20
        )

        if result.returncode != 0:
            print(f"[AI] curl 调用失败 (code={result.returncode}): {result.stderr[:200]}")
            return ""

        response = json.loads(result.stdout)
        summary = response.get("choices", [{}])[0].get("message", {}).get("content", "").strip()

        if summary:
            print(f"[AI] Deepseek 总结成功: {summary[:80]}...")
            return summary
        else:
            print("[AI] 响应为空，可能被内容过滤或 API 限制")
            return ""

    except subprocess.TimeoutExpired:
        print("[AI] curl 超时（超过20秒），跳过 AI 总结")
        return ""
    except json.JSONDecodeError as e:
        print(f"[AI] JSON 解析失败: {e}")
        # 打印部分 raw 输出帮助调试
        raw_preview = result.stdout[:300] if 'result' in dir() else ""
        if raw_preview:
            print(f"[AI] Raw response preview: {raw_preview}")
        return ""
    except Exception as e:
        print(f"[AI] 未知错误: {e}")
        return ""


def parse_manual_version(version_str, current_version_tuple):
    """尝试从重大改动行中解析手动版本号，失败则自动递增"""
    manual_re = re.compile(r'v(\d+)\.(\d+)\.(\d+)-(\w+)')
    m = manual_re.search(version_str)
    if m:
        return (int(m.group(1)), int(m.group(2)), int(m.group(3))), m.group(0)
    # 否则自动递增
    return bump_version(current_version_tuple)


def update_changelog(batches):
    """为每个批次（普通提交 + 1个重大改动）生成独立版本条目，优先 AI 发布说明"""
    if not batches:
        return False

    current_version_tuple, current_version_str = get_latest_version()

    with open(CHANGELOG_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 找到第一个 ## [vX.Y.Z-beta] 的位置，在此前插入
    first_version_match = CHANGELOG_VERSION_RE.search(content)

    today = datetime.now().strftime("%Y-%m-%d")
    new_entries = ""

    for batch in batches:
        # 找本批次中的重大改动提交
        major_entry = next((e for e in batch if "major_change" in e), None)
        if not major_entry:
            continue

        timestamp = major_entry["timestamp"]
        version_tuple, version_str = parse_manual_version(
            major_entry.get("major_change", ""), current_version_tuple
        )
        current_version_tuple = version_tuple

        # 对本批次所有提交调用 AI 总结
        ai_summary = summarize_with_deepseek(batch)

        if ai_summary:
            description = ai_summary
        else:
            # 回退：用重大改动标题
            description = major_entry.get("major_change", major_entry.get("summary", "更新"))

        new_entries += f"""## [{version_str}](https://github.com/{get_repo_name()}/releases/tag/{version_str}) — {today}

### ⚡ 重大改动 — {timestamp}

{description}

"""

    # 插入到第一个版本条目之前
    if first_version_match:
        insert_pos = first_version_match.start()
        content = content[:insert_pos] + new_entries + content[insert_pos:]
    else:
        content = content.rstrip() + "\n\n" + new_entries

    with open(CHANGELOG_PATH, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"[OK] CHANGELOG.md 已更新（追加 {len(batches)} 个版本条目）")
    return True


def get_repo_name():
    """获取当前仓库名，如 guilingzhouyi-creator/Small_WarThunder"""
    repo = os.environ.get("GITHUB_REPOSITORY", "guilingzhouyi-creator/Small_WarThunder")
    return repo


def commit_and_push(summary_text="DEVLOG / CHANGELOG 自动更新"):
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
        "-m", f"新增内容：{summary_text}",
        "-m", "改动及优化描述：自动化仓管 Agent 根据提交生成日志",
        "-m", "⚡内部操作：仓管自动提交"
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

    # ── 2. 解析并切割批次 ──
    devlog_entries = []
    batches = []          # 每个批次 = [普通提交...] + [1个重大改动提交]
    current_batch = []    # 当前正在收集的批次
    has_major = False

    for commit_hash, commit_date, msg in commits:
        parsed = parse_commit_message(commit_hash, msg, commit_date)
        if not parsed:
            print(f"[SKIP] {commit_hash[:8]} — 格式不符，跳过")
            continue

        devlog_entries.append(parsed)
        current_batch.append(parsed)

        if "major_change" in parsed:
            # 碰到重大改动 → 当前批次结束
            batches.append(current_batch)
            current_batch = []
            has_major = True
            print(f"[MAJOR] {commit_hash[:8]} — ⚡重大改动：{parsed['major_change']}")

        print(f"[OK] {commit_hash[:8]} — {parsed['timestamp']}")

    # 剩余普通提交没有重大改动跟随 → 丢弃批次（已写入 DEVLOG，不触发 CHANGELOG）
    if current_batch:
        print(f"[INFO] 末尾 {len(current_batch)} 条普通提交无后续重大改动，仅写入 DEVLOG，不产生版本条目")

    # ── 3. 决策：是否有重大改动？ ──
    if not devlog_entries:
        print("[INFO] 没有符合格式的提交")
        sys.exit(0)

    if not has_major:
        # 没有重大改动 → 不处理，不更新状态文件，等下次
        print(f"[INFO] 没有重大改动，跳过日志自动提交")
        print(f"[INFO] ({len(devlog_entries)} 条普通提交将在下次重大改动时批量写入)")
        sys.exit(0)

    # ── 4. 处理：更新 DEVLOG + CHANGELOG → 更新状态 → 提交推送 ──
    print(f"[ACTION] 检测到 {len(batches)} 个重大改动批次，开始处理范围内全部 {len(devlog_entries)} 条提交")

    devlog_updated = update_devlog(devlog_entries)

    # 自检归并历史 DEVLOG
    self_review_devlog()

    changelog_updated = update_changelog(batches)

    # 更新持久化状态到 HEAD
    save_last_processed(head_commit)

    if devlog_updated or changelog_updated:
        ai_summary = summarize_with_deepseek(devlog_entries[:1]) or "DEVLOG / CHANGELOG 自动更新"
        commit_and_push(summary_text=ai_summary)

    print(f"[DONE] 自动化仓管任务完成")
    print(f"  - 写入 DEVLOG：{len(devlog_entries)} 条")
    print(f"  - 写入 CHANGELOG：{len(batches)} 个版本条目")
    print(f"  - 状态已更新到：{head_commit[:8]}")


if __name__ == "__main__":
    main()
