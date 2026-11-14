"""Unity .asset 파일(YAML의 일부)만 읽는 작은 파서. 표준 라이브러리만 쓴다.

Unity가 쓰는 꼴만 다룬다.
- 블록 매핑(`key: value`), 목록(`- `, 부모 키와 같은 들여쓰기도 허용)
- 한 줄 흐름 매핑(`{fileID: 0, guid: ..., type: 2}`)과 빈 목록(`[]`)
- 여러 줄로 이어진 값(다음 줄이 더 깊이 들여쓰이면 이어 붙임)
"""

from __future__ import annotations

import re
from pathlib import Path

_INT = re.compile(r"-?(0|[1-9]\d*)")
_FLOAT = re.compile(r"-?\d+\.\d+(e-?\d+)?")
_KEY_LINE = re.compile(r"^[^{\[\"'][^:]*:( |$)")


class UnityYamlError(ValueError):
    """에셋 모양이 이 파서가 아는 꼴과 다를 때."""


def _scalar(text: str):
    text = text.strip()
    if text.startswith("{") and text.endswith("}"):
        inner = text[1:-1].strip()
        result = {}
        if inner:
            for part in inner.split(","):
                key, sep, value = part.partition(":")
                if not sep:
                    raise UnityYamlError(f"흐름 매핑을 읽을 수 없습니다: {text}")
                result[key.strip()] = _scalar(value)
        return result
    if text == "[]":
        return []
    if _INT.fullmatch(text):
        return int(text)
    if _FLOAT.fullmatch(text):
        return float(text)
    if len(text) >= 2 and text[0] == text[-1] and text[0] in "'\"":
        return text[1:-1]
    return text


def _lines(text: str) -> list[list]:
    result = []
    for raw in text.splitlines():
        if not raw.strip() or raw.startswith("%") or raw.startswith("---"):
            continue
        indent = len(raw) - len(raw.lstrip(" "))
        result.append([indent, raw.strip()])
    return result


def _block(lines: list[list], i: int, indent: int):
    if lines[i][1].startswith("-"):
        items = []
        while i < len(lines) and lines[i][0] == indent and lines[i][1].startswith("-"):
            rest = lines[i][1][1:].strip()
            if not rest:
                value, i = _block(lines, i + 1, lines[i + 1][0])
                items.append(value)
            elif _KEY_LINE.match(rest):
                lines[i] = [indent + 2, rest]
                value, i = _block(lines, i, indent + 2)
                items.append(value)
            else:
                items.append(_scalar(rest))
                i += 1
        return items, i
    mapping = {}
    while i < len(lines) and lines[i][0] == indent and not lines[i][1].startswith("-"):
        key, sep, value = lines[i][1].partition(":")
        if not sep:
            raise UnityYamlError(f"키가 없는 줄입니다: {lines[i][1]}")
        value = value.strip()
        i += 1
        if value == "":
            nested = i < len(lines) and (lines[i][0] > indent
                                         or (lines[i][0] == indent and lines[i][1].startswith("-")))
            if nested:
                child, i = _block(lines, i, lines[i][0])
            else:
                child = None
            mapping[key.strip()] = child
        else:
            while i < len(lines) and lines[i][0] > indent:
                value += " " + lines[i][1]
                i += 1
            mapping[key.strip()] = _scalar(value)
    if i < len(lines) and lines[i][0] > indent:
        raise UnityYamlError(f"들여쓰기가 맞지 않습니다: {lines[i][1]}")
    return mapping, i


def parse(text: str) -> dict:
    """에셋 글 전체를 읽는다. 맨 위가 `MonoBehaviour:` 하나면 그 안쪽을 돌려준다."""
    lines = _lines(text)
    if not lines:
        raise UnityYamlError("빈 에셋입니다")
    value, end = _block(lines, 0, lines[0][0])
    if end != len(lines):
        raise UnityYamlError(f"다 읽지 못했습니다: {lines[end][1]}")
    if isinstance(value, dict) and len(value) == 1:
        inner = next(iter(value.values()))
        if isinstance(inner, dict):
            return inner
    return value


def load_asset(path: Path) -> dict:
    return parse(path.read_text(encoding="utf-8"))


def read_guid(meta_path: Path) -> str | None:
    with meta_path.open(encoding="utf-8", errors="replace") as handle:
        for line in handle:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    return None
