# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)
| Severity | Concern                        | Evidence                        | Impact         | Suggested action         |
|----------|-------------------------------|----------------------------------|----------------|-------------------------|
| High     | No CI/CD pipeline             | Scan output                     | Missed errors  | Add GitHub Actions      |
| High     | No security config            | Scan output                     | Vulnerability  | Add SECURITY.md         |
| Medium   | Legacy code in BMTP3.Core     | Directory tree                  | Confusion      | Archive/clean up        |
| Medium   | No .env.example               | Scan output                     | Setup errors   | Add env template        |

### 2) Technical Debt
| Debt item           | Why it exists         | Where                | Risk if ignored | Suggested fix          |
|---------------------|----------------------|----------------------|-----------------|-----------------------|
| Legacy code         | Old implementation   | BMTP3.Core           | Confusion       | Archive/clean up      |
| TODOs in code       | Incomplete features  | See scan output      | Bugs            | Address TODOs         |

### 3) Security Concerns
| Risk                | OWASP category       | Evidence             | Current mitigation | Gap                  |
|---------------------|---------------------|----------------------|--------------------|----------------------|
| No security config  | N/A                 | Scan output          | None               | Add SECURITY.md      |

### 4) Performance and Scaling Concerns
| Concern             | Evidence            | Current symptom      | Scaling risk      | Suggested improvement |
|---------------------|---------------------|----------------------|-------------------|----------------------|
| [TODO]              | [TODO]              | [TODO]               | [TODO]            | [TODO]               |

### 5) Fragile/High-Churn Areas
| Area                | Why fragile         | Churn signal         | Safe change strategy |
|---------------------|---------------------|----------------------|---------------------|
| Directory.Build.props | Frequent changes  | Scan output          | Careful review      |
| Core2/BackupNew/Engine/BackupEngine.cs | Core logic | Scan output | Add tests           |

### 6) [ASK USER] Questions
1. What is the preferred mocking/isolation strategy?
2. Are there any required environment variables not discoverable from code?
3. What is the policy for error/logging conventions?
4. What is the coverage expectation?
