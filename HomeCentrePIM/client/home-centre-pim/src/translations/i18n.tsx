import i18next from 'i18next';
import HttpApi from 'i18next-http-backend';
import React from 'react';
import MasterDataService from '../services/MasterDataService';

const appConfig = new MasterDataService().GetAppConfig();

const options: any = {
  interpolation: {
    escapeValue: false, // not needed for react!!
  },

  initImmediate: false,
  debug: true,
  lng: 'en',
  fallbackLng: 'en',
  // have a common namespace used around the full app

  // ns: ['translations'],
  // defaultNS: 'translations',
  react: {
    wait: false,
    bindI18n: 'languageChanged loaded',
    bindStore: 'added removed',
    nsMode: 'default',
    defaultTransParent: 'div',
  },
};


export class ManageKeyPressEvents extends React.Component<any, any> {

  constructor(props: any) {
    super(props);
    new MasterDataService().InitializeAppConfig().then(r => {
      this.LoadTranslation(1);
    });
  }

  LoadTranslation = (key: any) => {
    const appConfig = new MasterDataService().GetAppConfig();
    const baseUrl = window.location.origin.includes('localhost:') ? API_ENDPOINT : appConfig.apiUrl;
    options['backend'] = {
      type: 'backend',
      crossDomain: false,
      allowMultiLoading: false,
      loadPath: baseUrl + '/api/Master/GetLabels?languageId=' + key + '&v=' + appConfig?.cacheVersion || new Date().getTime().toString(),
      parse: (data: any) => {
        const result: { [key: string]: string } = {};
        JSON.parse(data).result.forEach((x: any) => {
          result[x.name] = x.description
        })
        return result;
      },
      // loadPath: 'https://brf-demo-portal-1.azurewebsites.net/common/api/Master/GetTranslateMessageCodes?translateCode=' + key
    };
    i18next.use(HttpApi).init(options);
  };

  keydownHandler = (e: any) => {

    if (sessionStorage.getItem('isProduction') !== 'true' || sessionStorage.getItem('isProduction') === '') {
      if (e.ctrlKey) {
        if (e.keyCode === 77) {                 // 77 - M
          // this.LoadTranslation(1);
        } else if (e.keyCode === 89) {          // 89 - Y
          // this.LoadTranslation(0);
        } else if (e.keyCode === 71) {          // 71 - G
          for (var i = 0; i < sessionStorage.length; i++) {
            const key = sessionStorage.key(i) || '';
            if (key !== 'token') {
              sessionStorage.removeItem(key);
            }
          }
        } else if (e.shiftKey) {
          // this.LoadTranslation(new Date()?.getSeconds()?.toString());
        }
      }
    }
  };

  componentDidMount() {
    document.addEventListener('keydown', this.keydownHandler);
  }

  componentWillUnmount() {
    document.removeEventListener('keydown', this.keydownHandler);
  }

  render() {
    return ('');
  }
};
export default ManageKeyPressEvents;
