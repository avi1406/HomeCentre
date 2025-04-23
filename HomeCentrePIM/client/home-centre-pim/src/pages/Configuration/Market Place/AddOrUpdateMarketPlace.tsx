import { cancelIcon, iconButtonStyles } from '@/common/properties'
import { Marketplace } from '@/services/ApiClient'
import ApiManager from '@/services/ApiManager'
import NotificationManager from '@/services/NotificationManager'
import { IconButton, TextField } from '@fluentui/react'
import { Grid } from '@mui/material'
import { t } from 'i18next'
import { DefaultButton } from 'office-ui-fabric-react'
import React, { Component } from 'react'
import ReactFormValidation from "react-form-input-validation";
import { PIMcontentStyles, PIMfooterButtonCancel, PIMfooterButtonCancel1, PIMfooterButtonConfirm } from '../../PimStyles'

export default class AddOrUpdateMarketPlace extends Component<any, any> {
  form: ReactFormValidation
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  constructor(props: any) {
    super(props);
    this.state = {
      fields: {
        id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : "",
      },
      errors: {},
    }

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: "required|regex:/^[a-zA-Z0-9 \s]*$/",
    });

    this.form.onformsubmit = async (fields) => {
      this.props.CloseModal();
      var body = new Marketplace();
      body.id = fields.id;
      body.name = fields.name;
      body.shortCode = fields.name;
      body.isActive = true;
      this.apiClient.addOrUpdateMarketplace(body).then((response: any) => {
        let message = response.hasError ? response.message : fields.id == 0 ? `${t('MSG_ADD')}` : `${t('MSG_UPDATE')}`;
        if (response.hasError) {
          this.notify.showErrorNotify(message);
        } else {
          this.notify.showSuccessNotify(message);
        }
        this.props.response();
      })
    }
  }

  render() {
    return (
      <>
        <div className={PIMcontentStyles.header}>
          {/* <Grid container spacing={2}> */}
          {/* <Grid lg={10} item xs={9}> */}
          {this.props.fields.length == 0 ? (
            <span className="apptext1">{`${t('CREATE_MARKETPLACE')}`}</span>
          ) : (
            <span className="apptext1">{`${t('EDIT_MARKETPLACE')}`}</span>
          )}
          {/* </Grid> */}
          {/* <Grid item lg={0.5} xs={0.5}> */}
          {/* <IconButton
                        styles={iconButtonStyles}
                        iconProps={cancelIcon}
                        onClick={() => this.props.CloseModal()}
                    /> */}
          {/* </Grid> */}
          {/* <Grid item lg={1.5} xs={12} /> */}
          {/* </Grid> */}
        </div>
        <div className={PIMcontentStyles.body}>
          <form
            className="myForm"
            noValidate
            autoComplete="off"
            onSubmit={this.form.handleSubmit}
          >
            <div className={PIMcontentStyles.attributeBody}>
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={1} xs={12} />
                <Grid item lg={10} xs={12}>
                  <TextField
                    name="name"
                    required
                    className="textField"
                    label={`${t('MARKETPLACE_NAME')}`}
                    placeholder={`${t('MARKET_NAME_PLACEHOLDER')}`}
                    value={this.state.fields.name}
                    onBlur={this.form.handleBlurEvent}
                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                    errorMessage={this.state.errors.name ? this.state.errors.name : ''}
                  />
                </Grid>
                <Grid item lg={0.75} xs={12} />
              </Grid>
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    type="submit"
                    text={
                      this.state.fields.id == 0 ? `${t('BTN_SAVE')}` : `${t('EDIT_BTN_SUBMIT')}`
                    }
                    disabled={
                      this.props.fields.length == 1
                        ? !(this.props.fields[0].name == this.state.fields.name)
                          ? false
                          : true
                        : this.state.fields.name == ''
                          ? true
                          : false
                    }
                  />
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
                    onClick={() => this.props.CloseModal()}
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