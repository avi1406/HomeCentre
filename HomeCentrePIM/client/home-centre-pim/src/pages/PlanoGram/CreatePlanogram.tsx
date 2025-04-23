import { containerStackTokens, createPlanogram } from '@/common/properties';
import { IChoiceGroupOption, Stack } from '@fluentui/react';
import React, { Component, Suspense } from 'react'
import ReactFormValidation from 'react-form-input-validation';
import FormPlanogramOrFamily from './Planogram';
import { Text } from '@fluentui/react/lib/Text';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from "jwt-decode";
import { Grid } from '@mui/material';
import { PIMHearderText } from '../PimStyles';

class CreatePlanogram extends Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  token = sessionStorage.getItem("token");
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode?.RoleId.split(',');
  rolelvl: any = this.decode.RoleLevel;
  options: IChoiceGroupOption[] = [
    { key: 'A', text: 'Planogram' },
    { key: 'B', text: 'Family' }
  ];
  form: any;
  constructor(props: any) {
    super(props);
    this.state = {
      header: "Planogram",
      isRegistering: false,
      formType: null,
    }

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: "required|between:2,30|alpha_num",
      description: "required|between:2,30"
    });
  }

  componentDidMount() {

  }

  _onChange(ev: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) {
    this?.setState({ header: option.text });
  }

  render() {
    return (
      <div className="width-100">
        <React.Fragment>
          {(this.rolelvl == 'Level 2' && this.roleId.includes('5')) || this.roleId.includes('1') ? (
            <Grid container>
              <Grid item lg={3.5} md={10} xs={12}>
                {/* <Text block className="text">{`${this.props.t('CREATE_NEW_PLANOGRAM')}`}</Text> */}
                <FormPlanogramOrFamily />
              </Grid>
            </Grid>
          ) : (
            <Stack enableScopedSelectors tokens={containerStackTokens} styles={createPlanogram}>
              <Text
                variant="xxLarge"
                styles={PIMHearderText}
                className="color-blue text"
              >
                {`${this.props.t('NO_ACCESS_CREATE_PLANOGRAM')}`}
              </Text>
            </Stack>
          )}
        </React.Fragment>
      </div>
    );
  }
}
const ComponentTranslated = withTranslation()(CreatePlanogram);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;

