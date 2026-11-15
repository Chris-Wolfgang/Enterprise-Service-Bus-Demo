window.BENCHMARK_DATA = {
  "lastUpdate": 1794750103383,
  "repoUrl": "https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo",
  "entries": {
    "Mutation score": [
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "76fe1d29fbd50072999ebe836ba94923a7e33e07",
          "message": "fix(scripts): security-alerts updates the open summary issue instead of filing a new one (#158)\n\nGet-TrackedIssues keyed each marker to the LAST issue in the listing. The REST\nissues listing (state=all) is newest first, so with a closed old summary and an\nopen newer one the map kept the CLOSED issue, and every weekly run filed a\nduplicate summary issue. Prefer the OPEN issue for a marker, and between two of\nthe same state the higher number. Port of Chris-Wolfgang/repo-template#719.\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-10-15T17:38:27Z",
          "url": "https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/commit/76fe1d29fbd50072999ebe836ba94923a7e33e07"
        },
        "date": 1792306625151,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score (tests/EsbDemo.Core.Tests.Unit (defaults))",
            "value": 100,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Tests.Fuzz (defaults))",
            "value": 5.38,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Web.Tests.Unit (defaults))",
            "value": 62.6,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "d127cbdd8b4ddbbc84fa2c0412d43a80975d4893",
          "message": "changelog: add the missing fragment for #188's dependency bumps (#217)\n\n#188 changed src/ but merged without a changelog fragment.\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-11-04T18:36:43Z",
          "url": "https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/commit/d127cbdd8b4ddbbc84fa2c0412d43a80975d4893"
        },
        "date": 1794144592067,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score (tests/EsbDemo.Core.Tests.Unit (defaults))",
            "value": 100,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Tests.Fuzz (defaults))",
            "value": 5.38,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Web.Tests.Unit (defaults))",
            "value": 62.47,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "11c52cfe85bfc30ef6c4590690154ead1753bf41",
          "message": "build: test EsbDemo.Core on net11.0 and pin the SDK to 11.0 (#246)\n\n- tests/EsbDemo.Core.Tests.Unit: append net11.0 to TargetFrameworks (one\n  line) with the same package set as net10.0 (xunit 2.9.3, runner 3.1.5,\n  Test.Sdk 18.0.1, coverlet 10.1.0).\n- global.json: 10.0.100 -> 11.0.100, rollForward latestFeature. SDK 10\n  cannot build a net11.0 TFM (NETSDK1045); repo-template#831 names this\n  per-repo bump as the net11.0 issue's job.\n\nRefs #227.\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-11-14T14:29:21Z",
          "url": "https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/commit/11c52cfe85bfc30ef6c4590690154ead1753bf41"
        },
        "date": 1794750101320,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score (tests/EsbDemo.Core.Tests.Unit (defaults))",
            "value": 100,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Tests.Fuzz (defaults))",
            "value": 0,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/EsbDemo.Web.Tests.Unit (defaults))",
            "value": 0,
            "unit": "%"
          }
        ]
      }
    ]
  }
}