import React from 'react';

class Util {
  async asyncForEach<T>(array: T[], callback: any) {
    for (let index = 0; index < array.length; index++) {
      await callback(<T>array[index], index, array);
    }
  }

  humanFileSize(bytes: number, si = false, dp = 1) {
    const thresh = si ? 1000 : 1024;

    if (Math.abs(bytes) < thresh) {
      return bytes + ' B';
    }

    const units = si
      ? ['kB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB']
      : ['KiB', 'MiB', 'GiB', 'TiB', 'PiB', 'EiB', 'ZiB', 'YiB'];
    let u = -1;
    const r = 10 ** dp;

    do {
      bytes /= thresh;
      ++u;
    } while (Math.round(Math.abs(bytes) * r) / r >= thresh && u < units.length - 1);


    return bytes.toFixed(dp) + ' ' + units[u];
  }

  merge(obj1: any, obj2: any) {
    const answer: any = {};
    for (const key in obj1) {
      if (answer[key] === undefined || answer[key] === null)
        answer[key] = obj1[key];
    }
    for (const key in obj2) {
      if (answer[key] === undefined || answer[key] === null)
        answer[key] = obj2[key];
    }
    return answer;
  }

  phoneNoValid(value: string, country: any) {
    if (!value) {
      return true;
    }
    const input = value.substring(country.countryCode.length, value.length);
    if (!input) {
      return true;
    }
    if (!input || input.startsWith('0') || input.length < 7 || (input.startsWith('0') && input.length > 7)) {
      return false;
    } else {
      return true;
    }
  };

  validatePhone(rule: any, value: any) {
    if (!value || value.length >= 7) {
      return Promise.resolve();
    }
    return Promise.reject('Please enter valid phone!');
  };

  randomColorGenerator() {
    return '#' + (Math.random().toString(16) + '0000000').slice(2, 8);
  };

  getUrlParameterId() {
    return window.location.pathname?.substr(window.location.pathname?.lastIndexOf('/') + 1) || undefined; // .match(/\/([^/]*)$/)[1]
  };

  getFullDate(date: any) {
    if (date) {
      const d = new Date(date);
      const mlist = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
      const fullDate = d.getDate() + ' ' + mlist[d.getMonth()] + ' ' + d.getFullYear();
      return fullDate;
    } else {
      return "";
    }
  }

}

export default Util;
