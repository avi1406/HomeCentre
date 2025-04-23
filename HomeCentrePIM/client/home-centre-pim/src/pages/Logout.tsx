import React from 'react';
import AuthManagerService from '@/services/AuthManagerService';
import ApiManager from '@/services/ApiManager';

class Logout extends React.Component<any, any> {

  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  authToken = '';

  componentDidMount() {
    this.authManager.logout();
  }
  render() {
    return (
      <h3> Logging out ...</h3>
    );
  }
}

export default Logout;
