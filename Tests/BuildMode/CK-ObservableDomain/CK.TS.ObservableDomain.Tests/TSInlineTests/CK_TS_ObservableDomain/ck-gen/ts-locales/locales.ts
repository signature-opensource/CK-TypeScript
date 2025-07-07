export async function  loadTranslations(lang: string): Promise<{[key: string]: string}> {
    switch(lang) {
    default: return (await import('./en.json')).default;
  }
}

export const locales = {
  "en": { name: 'en', "nativeName": 'English', "englishName": 'English', "id": 221277614 },
}
