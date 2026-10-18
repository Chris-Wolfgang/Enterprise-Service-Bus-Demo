window.BENCHMARK_DATA = {
  "lastUpdate": 1792306627234,
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
      }
    ]
  }
}