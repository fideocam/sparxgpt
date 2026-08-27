# Security

EaGPT is a local Sparx EA add-in that sends a compact model digest to **Ollama** and, when the model replies with ArchiGPT-compatible JSON, applies those mutations to the open EA project.

This document is the threat model and the controls that are in place. It is not a pentest report.

## Trust boundaries

| Boundary | Who can influence it |
| --- | --- |
| EA model | Anyone who can edit the open project |
| Chat prompt | The EA user |
| Ollama URL / model name | The EA user, plus `%AppData%\EaGpt\settings.ini` |
| LLM reply | The selected Ollama model (and anyone who can poison its context) |
| COM add-in process | The Windows user who registered `/codebase` |

The LLM is **not** trusted. Parser, schema, and mutation-policy checks run before EA COM writes.

## Controls

### Ollama HTTP client

- Only `http` and `https`. `file:`, `ftp:`, `javascript:`, and other schemes are rejected.
- Userinfo (`user:password@host`) is rejected so credentials are not stored in settings or sent in `Host`.
- Path, query, and fragment are stripped; the client only calls `/api/tags` and `/api/chat` on the origin.
- Redirects are disabled (`AllowAutoRedirect = false`).
- Timeouts are clamped to 3s–600s.
- `HTTP_PROXY` is ignored so the model digest is not sent through a proxy.
- Model names cannot contain quotes, backslashes, or control characters (JSON injection into the request body).
- Request JSON escapes quotes, newlines, and other control characters.
- Well-known cloud metadata endpoints are blocked, including encodings:
  - `169.254.169.254` and the rest of `169.254.0.0/16`
  - IPv6-mapped `::ffff:169.254.169.254`
  - dword / hex / octal IPv4 forms of those addresses
  - `metadata.google.internal`, `metadata`, `instance-data`
  - Alibaba `100.100.100.200`
  - AWS IMDSv2 IPv6 `fd00:ec2::254`

Localhost and RFC1918 Ollama URLs remain allowed, matching ArchiGPT.

### Model mutations

- Replies larger than 200,000 characters are not treated as changes.
- `"elements"` in prose does not count as a change payload; the parser looks for `"elements": [` (and the other mutation keys).
- Element and relationship types must be ArchiMate 3 names (or known aliases). Unknown types never become EA stereotypes.
- Batch caps: 80 elements, 120 relationships, 50 removals, 256-character names, 80-character ids, diagram coordinates 0–4000.
- **Deletes from the model** (elements, relationships, whole diagrams) require an explicit Yes/No confirmation (default No).
- Remove-from-diagram-only is not treated as destructive (same as ArchiGPT).

### XML digest sent to the model

Element/relationship/diagram names are XML-escaped. Control characters are stripped so a hostile name in the EA model cannot break out of attributes in the prompt digest.

## Residual risks (accepted)

1. **Prompt injection via model content.** Names and notes in the EA project are sent to the LLM. A planted name can try to make the model emit change JSON. Mitigation: schema + limits + delete confirmation. **Adds still apply without a second prompt** if they validate. Review the chat transcript before continuing if the model is untrusted.
2. **SSRF to the LAN.** A user (or a tampered `settings.ini`) can point EaGPT at any http(s) host except the blocked metadata addresses. That is required for a networked Ollama box. Do not paste untrusted URLs into the Ollama field. Requests do not use `HTTP_PROXY`.
3. **DNS rebinding / newly registered names.** Hostname allowlisting is not used. Bind Ollama to localhost when you can.
4. **Unsigned COM `/codebase`.** `install.ps1` registers the DLL for the current user. Only load a build you compiled or otherwise trust.
5. **Ollama sees the model digest.** Treat the local model like any other process that can read the open architecture. Do not point EaGPT at a public hosted LLM unless that is acceptable.
6. **No EA COM tests in CI.** Linux CI covers `EaGpt.Core` only. The WinForms/COM importer is reviewed, not executed here.

## Reporting

Open a GitHub issue on [fideocam/sparxgpt](https://github.com/fideocam/sparxgpt) with the word **security** in the title. Do not attach live model extracts that you cannot share.
