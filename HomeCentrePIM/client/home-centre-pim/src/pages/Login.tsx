import React, { Suspense } from 'react';
import AuthManagerService from '@/services/AuthManagerService';
import router from 'umi/router';
import ApiManager from '@/services/ApiManager';
import { withTranslation } from 'react-i18next';
import { AdalConfig, AuthenticationContext, runWithAdal } from 'react-adal';
import { LoginViewModel } from '@/services/ApiClient';

class Login extends React.Component<any, any> {

  authManager = new AuthManagerService();
  apiClient = new ApiManager().CreateApiClient();

  constructor(props: any) {
    super(props);
    this.state = {
      isRegistering: false,
      formType: null,
    };
  }

  async componentDidMount() {
    const isAuthenticated = this.authManager.isAuthenticated();
    if (!isAuthenticated) {
      router.push('/login');
    } else {
      router.push('/overview');
    }
  }

  async login() {
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const loginSite = appConfiguration?.result?.find((e: any) => e.name == "IdentityProvider")?.value;
    if (loginSite == 'AzureAD') {
      window.location.href = '/overview'
    }
    else if (loginSite == 'b2c') {
      this.authManager.gotoB2CSignIn();
    }
  }

  render() {
    this.login();
    return (
      <>
        {/* <div >
          <div className="main_login"  >

            <ul className="cards_login">
              <li className="cards_item">
                <div className="card_login">
                  <div className="card_content">
                    <h2 className="card_title">Welcome to Home Centre - PIM</h2>
                    {/* <p className="card_text">Demo of pixel perfect pure CSS simple responsive card grid layout</p> */}
        {/* <button className="btn card_btn">Read More</button> */}
        {/* <div style={{ paddingTop: '10%', paddingLeft: '30%' }}>
                      <DefaultButton className="btn_login card_btn" style={{ height: '5%', width: '50%' }} type='primary' onClick={this.login.bind(this)}>
                        {this.props.t('BTN_LOGIN')}
                      </DefaultButton>
                    </div>

                  </div>
                </div>
              </li>
            </ul>
          </div>
        </div>  */}
      </>
    );
  }

  private setUserIsRegistering(b: boolean) {
    this.setState({ isRegistering: b });
  }
}

const ComponentTranslated = withTranslation()(Login);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;