import ApiManager from '@/services/ApiManager';
import { DefaultButton, TextField } from '@fluentui/react';
import React from 'react';
import ReactFormValidation from 'react-form-input-validation';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import NotificationManager from '@/services/NotificationManager';
import { PIMcontentStyles, PIMfooterButtonCancel, PIMfooterButtonCancel1, PIMfooterButtonConfirm } from '../PimStyles';

class AddorUpdateRole extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  form: ReactFormValidation;
  notify = new NotificationManager();
  constructor(props: any) {
    super(props);
    this.state = {
      fields: {
        id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : '',
        description: this.props.fields?.length == 1 ? this.props.fields[0].description : '',
        isActive: this.props.fields?.length == 1 ? this.props.fields[0].isActive : true,
      },
      errors: {},
      Ischeced: this.props.fields[0]?.isActive === true ? true : false,
      isActive1: this.props.fields[0]?.isActive === true ? true : false,
      checkInput: false,
    };
    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: 'required|between:2,30|regex:/^[a-zA-Z ]*$/',
      description: 'required|between:2,30',
      isActive: 'required|boolean',
    });
    this.form.onformsubmit = async (fields) => {
      try {
        this.props.ClosePopup();
        if (fields.id == 0) {
          const response = await this.apiClient.addRole(fields);
          this.setState({ selectedRejectComment: null, otherComment: false });
          if (response.hasError) {
            this.notify.showErrorNotify(response.message || this.props.t('ERROR_UNKNOWN'));
          } else {
            this.notify.showSuccessNotify(
              fields.id == 0 ? `${this.props.t('MSG_ADD')}` : `${this.props.t('MSG_UPDATE')}`,
            );
          }
          (error: any) => {
            this.notify.showErrorNotify(error);
          };
        } else {
          const response = await this.apiClient.updateRoles(
            fields.id,
            fields.name,
            fields.description,
            fields.isActive = this.state.Ischeced,
          );
          if (response.hasError) {
            this.notify.showErrorNotify(response.message || this.props.t('ERROR_UNKNOWN'));
          } else {
            this.notify.showSuccessNotify(
              fields.id == 0 ? `${this.props.t('MSG_ADD')}` : `${this.props.t('MSG_UPDATE')}`,
            );
          }
        }
        this.props.Refresh();
      } catch (e: any) {
        this.notify.showErrorNotify(e.message);
      }
    };
  }

  handleCheckboxChange = () => {
    this.setState({
      Ischeced: this.state.Ischeced == false ? true : false,
      checkInput: this.state.Ischeced === this.props.fields[0]?.isActive,
    });
  };

  render() {
    return (
      <>
        <div className={PIMcontentStyles.attributeBody}>
          <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
            {(this.props.fields?.length === 1 ? true : false) && (
              <Grid item lg={10} xs={12}>
                <div className={PIMcontentStyles.attributeBody2}>
                  <label>IsActive</label>
                  <input
                    type="checkbox"
                    name="isActive"
                    data-attribute-name="isActive"
                    className="checkbox"
                    data-async
                    checked={this.state.Ischeced}
                    onChange={this.handleCheckboxChange}
                  />
                </div>
              </Grid>
            )}
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12} />
              <Grid item lg={8} xs={12}>
                <TextField
                  required
                  label={`${this.props.t('ROLE_TABLE_NAME')}`}
                  name="name"
                  className="textField"
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields.name}
                  data-attribute-name="Name"
                  data-async
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                  errorMessage={
                    this.state.errors.name
                      ? this.state.errors.name == 'The Name format is invalid.'
                        ? 'The Name Allowes only Alphabets'
                        : this.state.errors.name
                      : ''
                  }
                />
              </Grid>
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12} />
              <Grid item lg={8} xs={12}>
                <TextField
                  label={`${this.props.t('ROLE_DESCRIPTION')}`}
                  name="description"
                  className="textField"
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields.description}
                  data-attribute-name="Description"
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                  data-async
                  errorMessage={this.state.errors.description ? this.state.errors.description : ''}
                />
              </Grid>
            </Grid>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    type="submit"
                    disabled={
                      this.props.fields.length == 1
                        ? !(this.props.fields[0].name == this.state.fields.name) ||
                          !(this.state.Ischeced === this.props.fields[0]?.isActive) ||
                          !(this.state.fields.description == this.props.fields[0].description)
                          ? false
                          : true
                        : this.state.fields.description == null || this.state.fields.name == ''
                    }
                  >
                    {this.props.t(this.state.fields.id != 0 ? 'EDIT_BTN_SUBMIT' : 'ADD_BTN_SUBMIT')}
                  </DefaultButton>
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    text={`${t('BTN_CANCEL')}`}
                    styles={
                      this.props.fields?.length === 1
                        ? PIMfooterButtonCancel1
                        : PIMfooterButtonCancel
                    }
                    onClick={this.props.ClosePopup}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </form>
        </div>
      </>
    );
  }
}
export default AddorUpdateRole;
