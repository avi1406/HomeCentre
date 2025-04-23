import CacheManager from './CacheManager';
import { ApiClient, AppConfiguration } from '@/services/ApiClient';
import axios, { AxiosInstance } from 'axios';
import AuthManagerService from '@/services/AuthManagerService';
import cacheObject from '@/services/CacheObject';
import { baseUrl } from './Constants';

/* global API_ENDPOINT:readonly */
class MasterDataService {

  cacheManager = new CacheManager();
  apiClient = new ApiClient();
  GetAppConfig() {
    const appConfigString = this.cacheManager.getItem('appConfig', null);
    return JSON.parse(appConfigString || '{}');
  }

  async InitializeAppConfig(): Promise<AppConfiguration> {
    const courtClient = new ApiClient(baseUrl, this.GetAxiosInstance(new Date().getTime().toString()));
    const output = await courtClient.getAppConfiguration();
    if (output.result) {
      output.result.apiUrl = baseUrl;
      this.cacheManager.setItem('appConfig', JSON.stringify(output.result));
    }
    return output;
  }


  GetAxiosInstance(cacheVersion: string): AxiosInstance {
    const token = sessionStorage.getItem('token');
    const instance = axios.create({
      headers: {
        token: token
      }
    });
    instance.interceptors.request.use((config: any) => {

      if (!cacheObject['ActiveFetchCount'] || cacheObject['ActiveFetchCount'] <= 0) {
        cacheObject['ActiveFetchCount'] = 1;
        document.body.classList.add('loading-indicator');
      } else {
        cacheObject['ActiveFetchCount']++;
      }
      if (config.method === 'get') {
        config.params = config.params || {};
        config.params['v'] = cacheVersion;
      }
      instance.defaults.headers.common['token'] = token;
      return config;
    }, function (error: Error) {
      return Promise.reject(error)
    })

    instance.interceptors.response.use(function (response) {
      // Any status code that lie within the range of 2xx cause this function to trigger
      // Do something with response data
      cacheObject['ActiveFetchCount']--;
      if (cacheObject['ActiveFetchCount'] <= 0) {
        document.body.classList.remove('loading-indicator');
      }
      return response;
    }, function (error: any) {
      // Any status codes that falls outside the range of 2xx cause this function to trigger
      // Do something with response error
      // notify.show(error.message);
      document.body.classList.remove('loading-indicator');
      // const notify = new NotificationManager();
      if (error && error.response && error.response.status === 401) {
        const authManager = new AuthManagerService();
        // notify.showWarnNotify('Session expired');
        // message.error('Session expired');
        // Modal.warning({
        //   title: 'Session Expired!',
        //   content: 'Your Session has Expired, Please login and try again!',
        //   keyboard: false,
        //   width: 600,
        //   onOk: () => {
        //     authManager.logout();
        //     window.location.href = '/login';
        //   },
        // });
        return new Promise(() => {
        });
      }

      if (error) {
        // notify.showErrorNotify(error.response?.data?.Message || error);
        // message.error(error.response?.data?.Message || error)
        // notify.showErrorNotify(error.message);
        // message.error(error.message);
        return new Promise(() => {
        });
      }

      return Promise.resolve(error); // Promise.reject(error);
    });
    return instance;
  }
}

export default MasterDataService;
