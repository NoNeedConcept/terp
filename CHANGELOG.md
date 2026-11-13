# Changelog

## [0.2.2](https://github.com/NoNeedConcept/terp/compare/v0.2.1...v0.2.2) (2026-11-13)


### Features

* add ci-setup and ci-post-build hooks for AOT verification ([953255e](https://github.com/NoNeedConcept/terp/commit/953255e83d71e17bdaf050439f8975e62eff8bdf))

## [0.2.1](https://github.com/NoNeedConcept/terp/compare/v0.2.0...v0.2.1) (2026-11-04)


### Features

* **terp:** add ValidationFailures for options validators ([ff388b6](https://github.com/NoNeedConcept/terp/commit/ff388b6918665a9ea3aabcc09e55afec77683e2e))


### Documentation

* mark AOT verification and commit done in archived options change ([55230ff](https://github.com/NoNeedConcept/terp/commit/55230ff1ef4e3434c861b48122ec882ffbae50f6))
* remove Migration.md, notes live in the consuming projects ([8e35145](https://github.com/NoNeedConcept/terp/commit/8e35145630c21aa039f18149ec0301a993ea2f14))
* update README examples to SetupChain fluent API ([31380ab](https://github.com/NoNeedConcept/terp/commit/31380ab2593c5d07df6fc7de9de2c119d146030d))

## [0.2.0](https://github.com/NoNeedConcept/terp/compare/v0.1.1...v0.2.0) (2026-11-02)


### ⚠ BREAKING CHANGES

* replace Setup extensions with SetupChain<T> fluent API

### Features

* replace Setup extensions with SetupChain&lt;T&gt; fluent API ([30aa625](https://github.com/NoNeedConcept/terp/commit/30aa625a138536ce5251cd64b8fa22422e022a47))


### Documentation

* update VitePress docs for SetupChain API ([6e2ea65](https://github.com/NoNeedConcept/terp/commit/6e2ea655b21022676a26cbf36419cc68ce282ebe))

## [0.1.1](https://github.com/NoNeedConcept/terp/compare/v0.1.0...v0.1.1) (2026-11-01)


### Features

* **akka:** add stream supervision resume extensions ([1774518](https://github.com/NoNeedConcept/terp/commit/17745185cefd6308ef5983804b8b1aecd105ef8a))


### Documentation

* rename Hosting section to Terp in navigation and move to /terp/ path ([9042b8f](https://github.com/NoNeedConcept/terp/commit/9042b8f1acf877fbf8ef7415737832b81851bc9a))

## 0.1.0 (2026-10-29)


### ⚠ BREAKING CHANGES

* reorganize Terp.Akka into Hosting and Entities namespaces

### Features

* add Terp and Terp.Akka libraries with tests and examples ([dce9dcc](https://github.com/NoNeedConcept/terp/commit/dce9dccb554f7aefc8b229d8a7577ab270194150))
* **akka:** add IActorRegistration for modular actor topology ([975fadf](https://github.com/NoNeedConcept/terp/commit/975fadf9ec4ebe0854cc538323b9b93d7c1de033))
* **diagnostics:** add CounterExtensions with Up/Down for Counter and UpDownCounter ([476b04c](https://github.com/NoNeedConcept/terp/commit/476b04ca8a1015551ce477b93ffd41a8913feb13))


### Bug Fixes

* correct repo URLs to NoNeedConcept org and raise Akka floor to 1.5.52 (CVE-2025-61778) ([30bee87](https://github.com/NoNeedConcept/terp/commit/30bee873688fc469fd05ae1f19320b2671a26e44))
* **docs:** accept any build configuration for XML doc paths ([c939987](https://github.com/NoNeedConcept/terp/commit/c939987222c804176624f4d3f896866711ef7aae))
* pin pnpm version and correct repo owner in docs ([72aaa5f](https://github.com/NoNeedConcept/terp/commit/72aaa5feaaff5534ec52b684b4d94cacbbcfe452))
* **test:** use AwaitAssertAsync for metrics assertions to avoid race condition ([57442d4](https://github.com/NoNeedConcept/terp/commit/57442d4c71fa07df25a840d6ab4e6886e2250b05))


### Documentation

* add documentation site with brand, API reference and llms.txt ([c2f6cc3](https://github.com/NoNeedConcept/terp/commit/c2f6cc3e0c0101f8e260332d079bdf9c3ad7a0b1))
* remove download and CI badges, clean up double dashes ([ed71b79](https://github.com/NoNeedConcept/terp/commit/ed71b795cb010d6d9baad0c1da5ca93169ff36e0))
* restructure helpers into Observability and Utilities groups ([219cbb9](https://github.com/NoNeedConcept/terp/commit/219cbb963059eb767dc7d237e8578c7a973d6c84))
* rewrite README and update docs for lockstep versioning ([eb1b50c](https://github.com/NoNeedConcept/terp/commit/eb1b50c04dcf5ee0b47225a55c323ed1c2e89bde))
* update namespace references for Terp.Akka.Hosting and Terp.Akka.Entities reorganization ([6d512b8](https://github.com/NoNeedConcept/terp/commit/6d512b81211b450a229ef67462bbeafc5e3934a5))


### Refactoring

* reorganize Terp.Akka into Hosting and Entities namespaces ([84fa019](https://github.com/NoNeedConcept/terp/commit/84fa019a74abc9b0513f38c867d57ff37795d156))
* use generic Setup&lt;T&gt;() in examples ([480e97f](https://github.com/NoNeedConcept/terp/commit/480e97f32dbc31ba5335cf16e2cc610d605cfafc))


### Dependencies

* update CodeCoverage and xunit, pin Verify.DiffPlex below 3.4.0 ([37eda66](https://github.com/NoNeedConcept/terp/commit/37eda6641e1f82a27b96be1d37b9af899ce05018))
