import { UrlParameterManager } from './UrlParameterManager';

export let isInited = false;
let localeIndex = 0;
let localeString = 'en-US';
let languageStrings: { [id: string]: string[] } = {};

export function initStrings(strings: { [id: string]: string[] }) {
  languageStrings = strings;
  isInited = true;
}

export function setLocale(locale = "en-US") {
  let _localeIndex = getLocaleIndex(locale);
  if (_localeIndex === -1) {
    locale = "en-US";
    _localeIndex = getLocaleIndex(locale);
  }
  localeString = locale;
  localeIndex = _localeIndex;
}

export function getLocale() {
  return localeString;
}

export function getLocaleIndex(locale: string) {
  return languageStrings.locales.indexOf(locale);
}

export function getAvailableLocales() {
  const locales = languageStrings.locales;
  const supportlist = ['en-US', 'sv-SE'];
  if (UrlParameterManager.getParams().alllocales) {
    return locales;
  }
  return locales.filter((l) => supportlist.indexOf(l) > -1);
}

export function getString(id = '', locale = '') {
  if (UrlParameterManager.getParams().showstringids === 'true') {
    return id;
  }
  const strings = languageStrings[id];
  const _localeIndex = locale !== '' ? getLocaleIndex(locale) : localeIndex;
  if (strings) {
    if (localeIndex < strings.length && strings[_localeIndex] != null) {
      const langString = strings[_localeIndex];
      if (langString === 'EMPTY') {
        return '';
      }
      return langString;
    } else {
      return "#" + strings[0];
    }
  }
  console.log("MISSING STRING", id);
  return 'MISSING:' + id;
}



