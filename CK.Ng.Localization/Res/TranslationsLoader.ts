import { Observable, from } from 'rxjs';
import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { loadTranslations } from '@local/ck-gen/ts-locales/locales';

export class CKTranslationsLoader implements TranslateLoader {
    getTranslation(lang: string): Observable<TranslationObject> {
        return from(loadTranslations(lang));
    }
}
