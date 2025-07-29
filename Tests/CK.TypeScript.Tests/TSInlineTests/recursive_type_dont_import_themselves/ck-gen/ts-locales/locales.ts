export async function loadTranslations(lang: string): Promise<{[key: string]: string}> {
    switch(lang) {
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
}
