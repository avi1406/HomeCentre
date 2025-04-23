import React from 'react';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';
import ApiManager from '@/services/ApiManager';
import { LoginViewModel } from '@/services/ApiClient';

class ParseToken extends React.Component<any, any> {

  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  authToken = '';
  private latitude: number | undefined;
  private longitude: number | undefined;

  componentDidMount() {
    this.authUsingB2c();
  }

  setAuthVariablesAndGotoHome(token: string | undefined | null) {
    sessionStorage.setItem('token', token || '');
    window.location.href = '/overview';
  }

  processToken() {
    if (this.authToken) {
      this.apiClient.authenticate(new LoginViewModel({
        token: this.authToken,
        latitude: this.latitude,
        longitude: this.longitude,
      })).then(response => {
        if (response !== null && !response.hasError) {
          this.setAuthVariablesAndGotoHome(response.result);
        } else {
          setTimeout(() => {
            this.authManager.logout();
          }, 3000);
        }
      });
    }
  }


  authUsingB2c() {
    const query = window.location.hash;
    let errorDescription = '';
    const params = new URLSearchParams(window.location.hash);
    const self = this;
    errorDescription = params.get('error_description') || '';
    if (errorDescription && errorDescription.includes('The user has forgotten their password')) {
      this.authManager.gotoB2CForgotPassword();
      return;
    }
    if (errorDescription) {
      this.authManager.gotoB2CSignIn();
      return;
    }
    if (query) {
      const token = query.substring(10);

      this.authToken = token;
      const jwt: any = jwt_decode(token || '');
      if ('geolocation' in navigator) {
        navigator.geolocation.getCurrentPosition(function (position) {
          self.latitude = position.coords.latitude;
          self.longitude = position.coords.longitude;
          self.processToken();
          return;
        });
        self.processToken();
      }
      else {
        self.processToken();
      }
    }

    if (this.authManager.isAuthenticated()) {
      // history.push('/home');
      window.location.href = '/overview';
    }
  }

  render() {
    return (
      <h1> Signing in ...</h1>
    );
  }
}

export default ParseToken;
