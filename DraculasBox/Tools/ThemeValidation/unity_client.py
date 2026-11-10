"""Use the project's already-running local Unity MCP over HTTP; no OS input."""
import argparse
import json
from pathlib import Path
import urllib.request


class UnityClient:
    def __init__(self):
        self.session = None
        self.counter = 0
        self.rpc("initialize", {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "dracula-theme-validation", "version": "1.0"},
        })
        self.rpc("notifications/initialized", notification=True)

    def rpc(self, method, params=None, notification=False):
        self.counter += 1
        message = {"jsonrpc": "2.0", "method": method}
        if not notification:
            message["id"] = self.counter
        if params is not None:
            message["params"] = params
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
        request = urllib.request.Request("http://127.0.0.1:8080/mcp", data=json.dumps(message).encode(), headers=headers)
        with urllib.request.urlopen(request, timeout=120) as response:
            self.session = response.headers.get("Mcp-Session-Id", self.session)
            raw = response.read().decode()
        if not raw:
            return None
        entries = [json.loads(line[6:]) for line in raw.splitlines() if line.startswith("data: ")]
        data = entries[-1] if entries else json.loads(raw)
        if "error" in data:
            raise RuntimeError(data["error"])
        return data.get("result")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--tool")
    parser.add_argument("--args-file", type=Path)
    parser.add_argument("--resource")
    options = parser.parse_args()
    client = UnityClient()
    if options.tool:
        arguments = json.loads(options.args_file.read_text(encoding="utf-8-sig")) if options.args_file else {}
        result = client.rpc("tools/call", {"name": options.tool, "arguments": arguments})
    elif options.resource:
        result = client.rpc("resources/read", {"uri": options.resource})
    else:
        result = client.rpc("tools/list")
        result = [{"name": item["name"], "schema": item.get("inputSchema")} for item in result["tools"] if item["name"] in {"execute_code", "refresh_unity", "read_console"}]
    print(json.dumps(result, ensure_ascii=False))
