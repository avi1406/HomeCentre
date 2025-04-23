import { BrowserRouter } from 'react-router-dom';
import React, { Suspense } from "react";
import { I18nextProvider, Translation, withTranslation } from "react-i18next";
import { initializeIcons } from "office-ui-fabric-react";
import i18next from 'i18next';
import ManageKeyPressEvents from '../translations/i18n';
import AppLayout from './AppLayout';
import ApiManager from '../services/ApiManager';
import AuthManagerService from '../services/AuthManagerService';
import MasterDataService from '@/services/MasterDataService';
import jwt_decode from "jwt-decode";
import Notifications from 'react-notify-toast';

class BasicLayout extends React.Component<any, any> {

  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;

  logout = () => {
    this.authManager.logout();
  };

  state = {
    collapsed: false,
    userrole: 0,
    loginSite: '',
  };

  onCollapse = (collapsed: any) => {
    this.setState({ collapsed });
  };

  toggle = () => {
    this.setState({
      collapsed: !this.state.collapsed,
    });
  };

  async componentDidMount() {
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const loginSite = appConfiguration?.result?.find((e: any) => e.name == "IdentityProvider")?.value;
    const loginJson = appConfiguration?.result?.find((e: any) => e.name == "IdentityProvider")?.jsonValue;
    this.setState({ loginSite: loginSite.toLowerCase() })
    if (loginSite.toLowerCase() == 'azuread') {
      if (!this.isAuthenticated) {
        this.authManager.gotoAzureAD(loginJson);
      }
    } else if (loginSite.toLowerCase() == 'b2c') {
      const secmail = sessionStorage.getItem("email")
      if (secmail) {
        this.updatemailverify();
        this.logout();
      }

      if (this.user?.isemailverified == false) {
        sessionStorage.setItem("email", this.user?.email || '');
        new MasterDataService().InitializeAppConfig().then(r => {
          return this.authManager.gotoB2CForgotPassword();
        });
      }

      if (this.user?.id) {
        const token = sessionStorage.getItem("token");
        const decode: any = jwt_decode(token || '');
        const roleId: any = decode?.RoleId.split(',');
        const roleLength = decode?.RoleId.split(',').length;
        for (var i = 0; i < roleLength; i++) {
          if (roleId[i].roleid == 1) {
            this.setState({ userrole: roleId[i].roleid });
            break;
          }
        }
      }
    }
  }

  updatemailverify() {
    const mail: any = sessionStorage.getItem("email");
    this.apiClient.ismail(mail);
  }

  render() {
    initializeIcons();
    return (
      <>
        <BrowserRouter>
          <I18nextProvider i18n={i18next}>
            <ManageKeyPressEvents />
            <Notifications />
            <Translation>
              {t => {
                return (
                  <div>
                    {this.state.loginSite == 'azuread' && this.isAuthenticated ?
                      < AppLayout
                        isAuthenticated={this.isAuthenticated}
                        userrole={this.state.userrole}
                        user={this.user}
                      />
                      :
                      this.state.loginSite == 'b2c' &&
                      < AppLayout
                        isAuthenticated={this.isAuthenticated}
                        userrole={this.state.userrole}
                        user={this.user}
                      />
                    }
                  </div>
                )
              }
              }
            </Translation>
          </I18nextProvider>
        </BrowserRouter>
      </>
    );
  }
}

const ComponentTranslated: any = withTranslation()(BasicLayout);
function App() {
  return (
    <Suspense fallback=''>
      <ComponentTranslated />
    </Suspense>
  )
}

export default App;
