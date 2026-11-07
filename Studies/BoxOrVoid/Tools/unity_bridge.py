"""Use the project's already-running local Unity MCP via streamable HTTP."""
import json
import pathlib
import sys
import requests


class UnityBridge:
    def __init__(self, url="http://127.0.0.1:8080/mcp"):
        self.url = url
        self.session = requests.Session()
        self.session.headers.update({"Accept": "application/json, text/event-stream"})
        self.sequence = 0
        self.request("initialize", {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "BoxOrVoidStudy", "version": "1.0"}})
        self.session.post(self.url, json={"jsonrpc": "2.0", "method": "notifications/initialized"}, timeout=15)

    def request(self, method, params=None):
        self.sequence += 1
        response = self.session.post(self.url, json={
            "jsonrpc": "2.0", "id": self.sequence, "method": method, "params": params or {}}, timeout=180)
        response.raise_for_status()
        session_id = response.headers.get("Mcp-Session-Id")
        if session_id:
            self.session.headers["Mcp-Session-Id"] = session_id
        if "text/event-stream" in response.headers.get("content-type", ""):
            messages = [json.loads(line[5:].strip()) for line in response.text.splitlines() if line.startswith("data:")]
            payload = next(m for m in messages if m.get("id") == self.sequence)
        else:
            payload = response.json()
        if "error" in payload:
            raise RuntimeError(payload["error"])
        return payload.get("result", {})


if __name__ == "__main__":
    bridge = UnityBridge()
    command = sys.argv[1]
    if command == "tools":
        result = bridge.request("tools/list")
    elif command == "resources":
        result = bridge.request("resources/list")
    elif command == "read":
        result = bridge.request("resources/read", {"uri": sys.argv[2]})
    elif command == "call":
        arg = sys.argv[3] if len(sys.argv) > 3 else "{}"
        if arg.startswith("@"):
            arg = pathlib.Path(arg[1:]).read_text(encoding="utf-8-sig")
        result = bridge.request("tools/call", {"name": sys.argv[2], "arguments": json.loads(arg)})
    else:
        raise SystemExit("tools | resources | read URI | call TOOL JSON_OR_@FILE")
    print(json.dumps(result, ensure_ascii=False, indent=2))
