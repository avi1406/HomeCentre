import MasterDataService from './MasterDataService';
import jwt_decode from 'jwt-decode';
import { LoginViewModel, MeModel } from '@/services/ApiClient';
import { AdalConfig, AuthenticationContext, runWithAdal } from 'react-adal';
import ApiManager from './ApiManager';
import { router } from 'umi';
import NotificationManager from './NotificationManager';

/* global API_ENDPOINT:readonly */
class AuthManagerService {

  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();

  isAuthenticated() {
    return !!sessionStorage.getItem('token');
  }

  gotoB2CSignIn() {
    sessionStorage.setItem('ReferralUrl', window.location.pathname);
    sessionStorage.setItem('AuthType', 'b2c');
    const baseUrl = window.location.origin.includes('localhost:') ? 'http://localhost:8000' : window.location.origin;
    const appConfig = new MasterDataService().GetAppConfig();
    const appUrl = window.location.origin.includes('localhost:') ? 'http://localhost:8000' : appConfig.appUrl || baseUrl;
    const B2cLogin = appConfig.b2C?.loginDomain ?? 'hcpimdev.b2clogin.com';
    const B2cTenant = appConfig.b2C?.tenant ?? 'hcpimdev.onmicrosoft.com';
    const LoginFlow = appConfig.b2C?.loginFlow ?? 'B2C_1_Login';
    const B2cClientId = appConfig.b2C?.clientId ?? '78e4a4f4-b63b-4029-b64b-c79c2adaf3b2';
    const B2cRedirectUri = appUrl || baseUrl;
    const url = 'https://' + B2cLogin + '/' +
      B2cTenant + '/oauth2/v2.0/authorize?p=' +
      LoginFlow + '&client_id=' +
      B2cClientId + '&nonce=defaultNonce&redirect_uri=' +
      B2cRedirectUri + '/parsetoken&scope=openid&response_type=id_token&prompt=login';
    window.location.href = url;
  }

  async gotoAzureAD(e: any) {
    const DO_NOT_LOGIN = false;
    const data = JSON.parse(e);
    const clientID = data[0].ClientID;
    const tenantID = data[1].TenantID;
    const adalConfig: AdalConfig = {
      tenant: tenantID,
      clientId: clientID,
      endpoints: {
        api: clientID,
      },
      postLogoutRedirectUri: window.location.origin,
      cacheLocation: 'localStorage' || 'sessionStorage'
    };
    const authContext = new AuthenticationContext(adalConfig);
    runWithAdal(
      authContext,
      async () => {
        authContext.acquireToken(clientID || '', async (message: any, tokens: any, msg: any) => {
          const token = localStorage.getItem("adal.idtoken") ? localStorage.getItem("adal.idtoken") : sessionStorage.getItem("adal.idtoken");
          var data = new LoginViewModel();
          data.token = token || undefined;
          const response: any = await this.apiClient.authenticate(data)
          if (response.hasError) {
            this.notify.showErrorNotify(response.message);
            this.logout();
          } else {
            sessionStorage.setItem('token', response.result || '');
            window.location.href = '/overview';
          }
        });
      },
      DO_NOT_LOGIN
    )

  }

  async logout() {
    sessionStorage.clear();
    localStorage.clear();
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const loginSite = appConfiguration?.result?.find((e: any) => e.name == "IdentityProvider")?.value;
    const baseUrl = window.location.origin.includes('localhost:') ? 'http://localhost:8000' : window.location.origin;
    window.location.href = loginSite.toLowerCase() == 'azuread' ? 'https://login.microsoftonline.com/common/oauth2/v2.0/logout?post_logout_redirect_uri=' + baseUrl : '/login';
  }

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  updateToken(token: string) {
    sessionStorage.setItem('token', token);
  }

  getBaseUrl(url: string): string {
    const pathArray = url.split('/');
    const protocol = pathArray[0];
    const host = pathArray[2];
    return protocol + '//' + host;
  }

  gotoB2CForgotPassword() {
    const baseUrl = window.location.origin.includes('localhost:') ? 'https://lmrkapppoc.z29.web.core.windows.net' : window.location.origin;
    const appConfig = new MasterDataService().GetAppConfig();
    const appUrl = window.location.origin.includes('localhost:') ? 'https://lmrkapppoc.z29.web.core.windows.net' : appConfig.appUrl || baseUrl;
    const B2cLogin = appConfig.b2C?.loginDomain ?? 'bifdevb2c.b2clogin.com';
    const B2cTenant = appConfig.b2C?.tenant ?? 'bifdevb2c.onmicrosoft.com';
    const ResetPasswordFlow = appConfig.b2C?.resetPasswordFlow ?? 'B2C_1_ForgotPassword';
    const B2cClientId = appConfig.b2C?.clientId ?? '78e4a4f4-b63b-4029-b64b-c79c2adaf3b2';
    const B2cRedirectUri = appUrl || baseUrl;
    const url = 'https://' + B2cLogin + '/' +
      B2cTenant + '/oauth2/v2.0/authorize?p=' +
      ResetPasswordFlow + '&client_id=' +
      B2cClientId + '&nonce=defaultNonce&redirect_uri=' +
      B2cRedirectUri + '/logout&scope=openid&response_type=id_token&prompt=login';
    window.location.href = url;
  }

  getUserData(): MeModel | null {
    const u = new MeModel();
    const jwt: any = jwt_decode(this.getToken() || '');
    if (jwt.UserData) {
      u.init(JSON.parse(jwt.UserData));
      return u;
    }
    return null;
  }

  getRoleId(): number {
    const jwt: any = jwt_decode(this.getToken() || '');
    return Number(jwt.RoleId);
  }

  getTenantId(): number | undefined {
    const jwt: any = jwt_decode(this.getToken() || '');
    if (!jwt.TenantId) {
      return;
    }
    return Number(jwt.TenantId);
  }
}

export default AuthManagerService;
