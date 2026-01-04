# XmlComparer Security Policy

XmlComparer is a .NET library and CLI for structural XML comparison. Because it operates on user‑provided XML, security is especially important.

## Supported Versions

In general, only the **latest released version** of XmlComparer receives security fixes. Critical issues may be back‑ported at the maintainer's discretion.

## Reporting a Vulnerability

If you believe you have found a security vulnerability in XmlComparer (for example related to XML parsing, entity expansion, external resources, or path handling):

1. **Do not** open a public GitHub issue.
2. Instead, use GitHub's private **"Report a vulnerability"** flow on the repository Security tab, or contact the maintainer privately via the contact details on their GitHub profile.
3. Provide as much detail as possible, including:
   - XmlComparer version in use (NuGet package version or commit SHA)
   - How you are using XmlComparer (library, CLI runner, or both)
   - .NET runtime version and OS
   - Minimal XML samples or commands that reproduce the issue
   - The potential impact you see (e.g., denial of service, information disclosure)

We will acknowledge receipt of your report as soon as reasonably possible and work with you to understand and address the issue.

## Areas of Particular Interest

When reporting, please call out if the issue affects any of the following:

- XML parsing behavior (e.g., entity expansion, DTDs, external resources)
- Schema validation (`XmlSchemaValidator`, `XmlSchemaSetFactory`)
- File and path handling in the CLI runner (`XmlComparer.Runner`)
- Resource usage that could lead to denial‑of‑service scenarios

## Disclosure

Once a fix is available, a GitHub security advisory and/or release notes entry may be published so users can update safely.
