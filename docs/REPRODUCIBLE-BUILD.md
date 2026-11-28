# Verifying a release build

A release of Enterprise-Service-Bus-Demo is meant to be **reproducible**: building its tag with the
SDK the release recorded, on Linux, at the path the release recorded, gives files byte-identical to
the ones inside the release's zips. This page shows how to check that for yourself. It follows the
fleet standard (claude-sessions `standards/reproducible-builds.md`, app variant decided 2026-11-20,
#221): this repository publishes no NuGet package, so the artifacts compared are the published
application files.

## What each release carries

| Asset | What it is |
|---|---|
| `EsbDemo.Web-<tag>-<rid>.zip` | A self-contained publish of `EsbDemo.Web` for one runtime identifier. The apps and runtimes are declared once in [`.github/release-apps.json`](../.github/release-apps.json): today `win-x64`, `linux-x64` and `osx-arm64`. |
| `SHA256SUMS.txt` | One `sha256sum`-format line per file **inside** each zip, keyed `<archive-name>/<path>` (for example `EsbDemo.Web-v1.0.0-linux-x64/EsbDemo.Web.dll`), then one line per zip keyed by its file name. |
| `BUILD-MANIFEST.txt` | The tag, commit, build path and build flags, and for each zip its project, runtime, publish flags, exact SDK version and runner image. |
| `Enterprise-Service-Bus-Demo-app-<tag>.intoto.jsonl` | The SLSA build-provenance bundle. The zips, `SHA256SUMS.txt` and `BUILD-MANIFEST.txt` are all its subjects. |

The zip files themselves are **not** reproducible, because each carries the time it was made. Their
lines in `SHA256SUMS.txt` only confirm that your download is intact. A rebuild is compared on the
files inside.

## Check a download

```bash
gh attestation verify EsbDemo.Web-<tag>-linux-x64.zip --repo Chris-Wolfgang/Enterprise-Service-Bus-Demo
gh attestation verify SHA256SUMS.txt --repo Chris-Wolfgang/Enterprise-Service-Bus-Demo
sha256sum -c --ignore-missing SHA256SUMS.txt
```

Run the last line in the folder holding the downloaded zips: it checks every zip that is present and
skips the per-file lines, whose files are inside the zips.

## Check a rebuild

Run this on Linux (the releases are built on `ubuntu-latest`).

1. **Download** `SHA256SUMS.txt` and `BUILD-MANIFEST.txt` from the release.
2. **Clone the tag at the manifest's `Build path:`** (on GitHub-hosted runners,
   `/home/runner/work/Enterprise-Service-Bus-Demo/Enterprise-Service-Bus-Demo`). The path matters:
   the Razor source generator writes the absolute path of each `.razor` file into the generated code it
   embeds in `EsbDemo.Web.pdb`, so a build from any other directory gives a different `.pdb`, and a
   different `EsbDemo.Web.dll`, which records the `.pdb`'s id. A container makes this easy:

   ```bash
   git clone --branch <tag> https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo.git
   docker run --rm -it \
     -v "$PWD/Enterprise-Service-Bus-Demo:/home/runner/work/Enterprise-Service-Bus-Demo/Enterprise-Service-Bus-Demo" \
     -w /home/runner/work/Enterprise-Service-Bus-Demo/Enterprise-Service-Bus-Demo \
     mcr.microsoft.com/dotnet/sdk:<major> bash
   ```

   Use a real git clone, not a source zip: SourceLink reads `.git` to map source paths.
3. **Install the SDK the manifest names** (`SDK:` lines). `global.json` pins the major version with
   `rollForward: latestFeature`; install the exact one shown so the compiler is the same.
4. **Publish** each archive exactly as `release.yaml` does, with `CI=true` (GitHub Actions sets it;
   it turns on `ContinuousIntegrationBuild`). Take the flags from the manifest; for `linux-x64`:

   ```bash
   export CI=true
   tag=<tag>; rid=linux-x64
   dotnet publish src/EsbDemo.Web/EsbDemo.Web.csproj \
     --configuration Release --runtime "$rid" --self-contained true \
     -p:PublishSingleFile=false \
     --output "publish/EsbDemo.Web-$tag-$rid"
   ```

5. **Compare** from inside `publish/`, where the `<archive-name>/` keys resolve:

   ```bash
   cd publish
   sha256sum -c --quiet --ignore-missing /path/to/SHA256SUMS.txt && echo "reproduced"
   ```

   Expect exactly one `FAILED` line per archive, for `EsbDemo.Web.staticwebassets.endpoints.json`
   (see below). Check that file with its `Last-Modified` values blanked on both sides:

   ```bash
   blank() { sed -E 's/("Name":"Last-Modified","Value":")[^"]*"/\1"/g' "$1"; }
   diff <(blank EsbDemo.Web-$tag-$rid/EsbDemo.Web.staticwebassets.endpoints.json) \
        <(unzip -p /path/to/EsbDemo.Web-$tag-$rid.zip EsbDemo.Web.staticwebassets.endpoints.json | blank /dev/stdin) \
     && echo "endpoints manifest matches apart from Last-Modified"
   ```

## What must match, and what may not

- **Must match:** every file listed in `SHA256SUMS.txt` except the one below, rebuilt on Linux with the
  recorded SDK at the recorded path. Measured before the first hashed release (commit `f06fa0c`, SDK
  11.0.100, a `linux-x64` self-contained publish run on Windows): two clean publishes in the same
  directory gave 363 files, all byte-identical except `EsbDemo.Web.staticwebassets.endpoints.json`, which matched once its
  `Last-Modified` values were blanked.
- **Differs on every build:** the `Last-Modified` response-header values in
  `EsbDemo.Web.staticwebassets.endpoints.json`. The SDK writes each static asset's file time into that
  manifest, and the compressed assets are written at build time, so the values are the build clock.
  The rest of that file (routes, ETags, content lengths) must match.
- **Differs from another directory:** `EsbDemo.Web.dll` and `EsbDemo.Web.pdb`, because of the Razor
  absolute paths described in step 2. `EsbDemo.Core` is not affected.
- **May differ:** the zip containers (timestamps), and anything built on another operating system.
  The **Reproducible Build** check publishes on Windows too and reports, without failing, how many
  files match the Ubuntu publish. Cross-OS identity is reported, not promised.
- **Releases cut before #221 landed** have no `SHA256SUMS.txt` or `BUILD-MANIFEST.txt`.

## How it is kept true

- `Directory.Build.props`: a literal copyright year, never the build clock; `ContinuousIntegrationBuild`
  when `CI=true`; SourceLink.
- `global.json`: pins the SDK major version.
- `.github/release-apps.json`: the one list of apps, runtimes and publish flags that both workflows read.
- `.github/workflows/reproducible-build.yaml`: on every pull request, builds every `src/` project and
  publishes every declared app and runtime twice on Ubuntu, `git clean -xdf` between, and fails if any
  assembly, PDB or published file differs (the endpoints manifest compared with `Last-Modified`
  blanked).
- `.github/workflows/release.yaml`: hashes the exact files it zips, in the same job, and attaches the
  hashes and manifest to the release, attested with the zips.
