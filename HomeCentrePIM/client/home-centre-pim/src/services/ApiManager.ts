import axios, { AxiosInstance, AxiosResponse } from 'axios';
import AuthManagerService from './AuthManagerService';
import CacheManager from './CacheManager';
import CacheObject from './CacheObject';
import MasterDataService from './MasterDataService';
import { ApiClient, AppConfiguration } from '@/services/ApiClient';
import NotificationManager from './NotificationManager';
import { baseUrl } from './Constants';

class ApiManager {
  cacheManager = new CacheManager();
  masterData = new MasterDataService();

  CreateApiClient(): ApiClient {
    const appConfig = this.masterData.GetAppConfig();
    return new ApiClient(appConfig.apiUrl || baseUrl, this.GetAxiosInstance(appConfig?.cacheVersion || new Date().getTime().toString()));
  }

  async InitializeAppConfig(): Promise<AppConfiguration> {
    const courtClient = new ApiClient(baseUrl, this.GetAxiosInstance(new Date().getTime().toString()));
    const output = (await courtClient.getAppConfiguration()).result;
    if (output) {
      output.apiUrl = baseUrl;
      this.cacheManager.setItem('appConfig', JSON.stringify(output));
      return output;
    } else {
      return new AppConfiguration();
    }
  }

  Download(urlPart: string, postData: any): Promise<AxiosResponse<any>> {
    const appConfig = this.masterData.GetAppConfig();
    const axiosInstance = this.GetAxiosInstance(appConfig?.cacheVersion || new Date().getTime().toString());
    return axiosInstance.post(appConfig.apiUrl + urlPart, postData, {
      responseType: 'blob',
    });
  }

  async UploadFile(path: string, formData: FormData, file: File) {
    document.body.classList.add('loading-indicator');
    formData.append('uploadedFile', file);
    const requestHeaders: HeadersInit = new Headers();
    requestHeaders.set('token', sessionStorage.getItem('token') || '');
    requestHeaders.set('questionSetName', formData.get('questionSetName')?.toString() || '');
    const response = await fetch(
      window.location.origin + path,
      {
        method: 'POST',
        headers: requestHeaders,
        body: formData,
      },
    );
    const result = await response.json();
    document.body.classList.remove('loading-indicator');
    return result;
  }

  GetAxiosInstance(cacheVersion: string): AxiosInstance {
    const token = sessionStorage.getItem('token');
    const instance = axios.create({
      headers: {
        token: token,
      },
    });
    instance.interceptors.request.use((config: any) => {
      config.cancelToken = axios.CancelToken.source().token;
      if (!CacheObject['ActiveFetchCount'] || CacheObject['ActiveFetchCount'] <= 0) {
        CacheObject['ActiveFetchCount'] = 1;
        document.body.classList.add('loading-indicator');
      } else {
        CacheObject['ActiveFetchCount']++;
      }
      if (config.method === 'get') {
        config.params = config.params || {};
        config.params['v'] = cacheVersion;
      }
      instance.defaults.headers.common['token'] = token;
      return config;
    }, function (error: Error) {
      return Promise.reject(error);
    });

    instance.interceptors.response.use(function (response) {
      if (response.data.hasError) {
        const errorMessage = response.data.message;
        const ignoredErrorMessages = [
          `Invalid itemcode`,
          `Uid is inactive or not exist for this`,
          `Can't able to create, Due to the itemcode is from different division`,
          `CAM is not available for this`,
          `Attributes Incomplete for`,
          `is already exists`
        ];
        const shouldIgnoreError = ignoredErrorMessages.some(ignoredMessage => errorMessage.includes(ignoredMessage));
        if (!shouldIgnoreError) {
          const notify = new NotificationManager();
          notify.showErrorNotify(errorMessage);
        }
      }
      // Any status code that lie within the range of 2xx cause this function to trigger
      // Do something with response data
      CacheObject['ActiveFetchCount']--;
      if (CacheObject['ActiveFetchCount'] <= 0) {
        document.body.classList.remove('loading-indicator');
      }
      return response;
    }, function (error: any) {
      // Any status codes that falls outside the range of 2xx cause this function to trigger
      // Do something with response error
      const notify = new NotificationManager();
      notify.showErrorNotify(error.message);
      document.body.classList.remove('loading-indicator');
      if (error && error.response && error.response.status === 401) {
        const authManager = new AuthManagerService();
        notify.showWarnNotify('Session expired');
        return new Promise(() => {
        });
      }

      if (error) {
        // notify.showErrorNotify(error.response?.data?.Message || error);
        // notify.showErrorNotify(error.message);
        return new Promise(() => {
        });
      }

      return Promise.resolve(error); // Promise.reject(error);
    });
    return instance;
  }
}

export default ApiManager;
