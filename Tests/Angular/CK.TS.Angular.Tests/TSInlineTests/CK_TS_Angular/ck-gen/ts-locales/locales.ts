export async function  loadTranslations(lang: string): Promise<{[key: string]: string}> {
    switch(lang) {
    case 'fr': return (await import('./fr.json')).default;
    case 'en-gb': return (await import('./en-gb.json')).default;
    case 'en-us': return (await import('./en-us.json')).default;
    default: return (await import('./en.json')).default;
  }
}
export type LocaleInfo = {
  name: string;
  nativeName: string;
  englishName: string;
  id: number;
};

export type CKLocales = {
  [localeCode: string]: LocaleInfo;
};

export const locales: CKLocales = {
  "en": { name: 'en', "nativeName": 'English', "englishName": 'English', "id": 221277614 },
  "fr": { name: 'fr', "nativeName": 'français', "englishName": 'French', "id": 210333265 },
  "en-gb": { name: 'en-gb', "nativeName": 'English (United Kingdom)', "englishName": 'English (United Kingdom)', "id": -1220541402 },
  "en-us": { name: 'en-us', "nativeName": 'English (United States)', "englishName": 'English (United States)', "id": -1255733531 },
}
