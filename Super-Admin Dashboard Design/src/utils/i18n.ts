import en from '../locales/en.json';
import ar from '../locales/ar.json';

type Language = 'en' | 'ar';
type TranslationKeys = keyof typeof en;

const translations: Record<Language, any> = {
  en,
  ar
};

export const getTranslation = (key: string, lang: Language): string => {
  return translations[lang][key] || key;
};

export const setDocumentDirection = (lang: Language) => {
  document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  document.documentElement.lang = lang;
};
