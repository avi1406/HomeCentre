import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Grid } from '@mui/material';
import { Modal } from '@fluentui/react';
import { Text } from '@fluentui/react/lib/Text';
import AuthManagerService from '@/services/AuthManagerService';
import {
  SignOutRegular,
  ChevronUpFilled,
  ChevronDownFilled,
  PersonCircleFilled,
} from '@fluentui/react-icons';
import axios from 'axios';
import jwt_decode from 'jwt-decode';
import { baseUrl } from '@/services/Constants';
import ApiManager from '@/services/ApiManager';
import { HelpEmail } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';

class AppHeader extends React.Component<any, any> {
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  username: any = this.user?.name || '';
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();

  constructor(props: any) {
    super(props);
    this.state = {
      isRegistering: false,
      formType: null,
      NotificationVisible: false,
      isMenuVisible: false,
      HelpEmail: '',
    };
  }

  componentDidMount() {
    document.addEventListener('click', this.handleClickOutside);
    this.fetchHelpEmail();
  }

  componentWillUnmount() {
    document.removeEventListener('click', this.handleClickOutside);
  }

  handleClickOutside = (event: any) => {
    const menuNode = document.getElementById('userMenu');
    const logoutNode = document.getElementById('logoutLink');
    if (!menuNode?.contains(event.target) && !logoutNode?.contains(event.target)) {
      this.setState({ isMenuVisible: false });
    }
  };

  handleLogoClick() {
    setTimeout(() => {
      window.location.href = '/overview';
    }, 10);
  }

  fetchHelpEmail = async () => {
    try {
      const apiget = await this.apiClient.getHelpEmail();
      var apiget1 = apiget.result?.[0].name;
      this.setState({ HelpEmail: apiget1 });
    } catch {
      this.notify.showErrorNotify('user not found');
    }
  };

  showNotification = () => {
    this.setState({ NotificationVisible: true });
  };

  hideNotification = () => {
    this.setState({ NotificationVisible: false });
  };

  render() {
    return (
      <>
        <div className="appheade">
          <div className="headerStrip">
            <Grid lg={12} item container spacing={0.3} direction={'row'}>
              <Grid lg={11} md={10} xs={9}></Grid>
              <Grid lg={0.5} md={0.5} xs={1.5}>
                <Link to={undefined}>
                  <p className="headerStripContext">FAQ</p>
                </Link>
              </Grid>
              <Grid lg={0.5} md={0.5} xs={1.5}>
                <Link to={undefined}>
                  <p className="headerStripContext" onClick={this.showNotification}>
                    Help
                  </p>
                </Link>
              </Grid>
            </Grid>
          </div>
          <div
            style={{
              backgroundColor: 'white',
              height: '44px',
              position: 'relative',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <Link to={{}} onClick={this.handleLogoClick}>
              <img
                src="https://sthomecentredlsgen2.blob.core.windows.net/images/image.jpg"
                style={{ marginLeft: '0.1px', marginTop: '3px', height: '40px' }}
                width={210}
              />
            </Link>
            <div
              className="headerMenuTrigger"
              id="userMenu"
              onClick={() => {
                this.setState({ isMenuVisible: !this.state.isMenuVisible });
              }}
            >
              <PersonCircleFilled style={{ fontSize: 24 }} />
              <span className="username" style={{ fontSize: 16 }}>
                {this.username}
              </span>
              <span>
                {this.state.isMenuVisible ? (
                  <ChevronDownFilled style={{ fontSize: 20 }} />
                ) : (
                  <ChevronUpFilled style={{ fontSize: 20 }} />
                )}
              </span>
            </div>
            {this.state.isMenuVisible && (
              <div className="headerMenuPopover">
                <Link id="logoutLink" to={{ pathname: `/logout` }} className="unstyled-link">
                  <SignOutRegular style={{ fontSize: 25, marginRight: '7px', marginLeft: '8px' }} />
                  Logout
                </Link>
              </div>
            )}
          </div>

          <Modal
            isOpen={this.state.NotificationVisible}
            containerClassName="customModalContainer"
            isBlocking={false}
            onDismiss={this.hideNotification}
          >
            <div>
              <Text block variant="mediumPlus" className="appNotification">
                If you have any issues, please contact the administrator -
                <br />
                <a href={`mailto:${this.state.HelpEmail}`} target="_blank" className="appEmail">
                  {this.state.HelpEmail}
                </a>
              </Text>
            </div>
          </Modal>
        </div>
      </>
    );
  }
}

const ComponentTranslated = withTranslation()(AppHeader);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
