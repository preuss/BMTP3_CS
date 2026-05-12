| Kategori                | Core (legacy)                                   | Core2 (pipeline)                                   | Core3 (simpel)                                   |
|------------------------|------------------------------------------------|----------------------------------------------------|--------------------------------------------------|
| Arkitektur             | Monolitisk, God objects                        | Pipeline, parallelisme, channels                   | Sekventiel, modulær, DI                          |
| Fejlhåndtering         | Fail-fast, stopper på fejl                     | Kompliceret, race conditions, deadlocks            | Robust, differentierer kritiske/ikke-kritiske    |
| State management       | Implicit, nullable properties                  | Eksplicit, men kompleks                            | Enkel, eksplicit                                 |
| Sidecar/metadata       | Tæt koblet, svært at udskifte                  | Udskilt, men afhængig af pipeline                  | Udskilt, DI                                      |
| Performance            | OK, men ikke skalerbar                         | Hurtig, men ustabil ved MTP                        | Langsom, men robust                              |
| Udvidelsesmuligheder   | Svært, tæt koblet                              | Bedre, men kræver pipeline-ændringer               | Let, modulært                                    |
| Testbarhed             | Lav, svært at isolere                          | OK, men stages kan være komplekse                  | Høj, små moduler                                 |
| Typiske problemer      | Vedligeholdelse, fejlpropagering               | Deadlocks, race conditions, fejlhåndtering         | Performance, manglende features                  |

**Forklaring:**
- Tabellen sammenligner nøgleaspekter på tværs af versionerne.
- Core: Monolitisk, svært at vedligeholde, fail-fast.
- Core2: Pipeline og parallelisme, men kompleksitet og ustabilitet.
- Core3: Simpel og robust, men mangler performance og features.
- Core4 bør kombinere modularitet, robusthed og performance.