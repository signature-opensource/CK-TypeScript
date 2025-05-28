# CK.Ng.Zorro

This package wraps [ng-zorro-antd](https://ng.ant.design/) and provides shared services, styles, and utilities designed to be used across all angular projects.

## Dependencies

Depends on:

- [CK.TS.Angular](https://github.com/signature-opensource/CK-TypeScript/tree/develop/CK.TS.Angular)
- [CK.Ng.Localization](https://github.com/signature-opensource/CK-TypeScript/tree/develop/CK.Ng.Localization)

## Features

- Preconfigured `ng-zorro-antd` package
- Shared services and utilities
- Global styles and themes
- Provides ngx-translate support, with ts-locales loading

## Installation

Just install CK.Ng.Zorro package in your dotnet project, and voila.

After running CK-Build :

- The global `@import 'ng-zorro-antd/ng-zorro-antd.less` will appear in your /src/styles.less global stylesheet.
- A default `provideNzI18n( fr_FR )` provider will be added to CKGenModule.Providers. The library's components will be translated to french by default (cf. <https://ng.ant.design/docs/i18n/en>).
- A `provideTranslateService( { defaultLanguage: 'fr', loader: { provide: TranslateLoader, useClass: CKTranslationsLoader } }` will be added to CKGenModule.Provider (cf. <https://github.com/signature-opensource/CK-TypeScript/tree/develop/CK.Ng.Localization>). Just like above, french will be the default loaded translations.

> Note: French was chosen as default language to enhance `package first` vision.
