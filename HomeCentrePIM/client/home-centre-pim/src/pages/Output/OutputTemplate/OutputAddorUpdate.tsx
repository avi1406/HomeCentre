import { iconButtonStyles } from '@/common/Button';
import { cancelIcon } from '@/common/properties';
import { MarketplaceTemplate } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import { Grid } from '@mui/material';
import ReactFormValidation from 'react-form-input-validation';
import { DefaultButton, IconButton } from 'office-ui-fabric-react';
import React, { Component } from 'react';
import { PIMcontentStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm } from '../../PimStyles';
import AuthManagerService from '@/services/AuthManagerService';
import { t } from 'i18next';
import { Dropdown, TextField } from '@fluentui/react';
import NotificationManager from '@/services/NotificationManager';
import jwt_decode from 'jwt-decode';

export default class OutputAddOrUpdateTemplate extends Component<any, any> {
  form: ReactFormValidation;
  authManager = new AuthManagerService();
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  divisionId: any = this.decode?.DepartmentId?.split(',');
  divisionLength = this.divisionId.length;

  constructor(props: any) {
    super(props);
    this.state = {
      Template: false,
      searchValue: '',
      errors: {},
      templateName: '',
      msgVisible: false,
      message: '',
      messageType: false,
      roleOption: [],
      roleSelected: this.props.selectedRole == undefined ? [] : this.props.selectedRole,
      role: [],
      fields: {
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : null,
      },
      isActive: true,
      ArrayData: [],
      divisionId: null,
      productDept: this.props.fields?.length == 1 ? this.props.fields[0].productDepartmentId : null,
    };

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: 'required',
      division: 'required',
      departmentId: 'required',
    });
    this.form.onformsubmit = async (fields: any) => {
      this.props.CloseModal();
      for (var i = 0; i < this.divisionLength; i++) {
        if (this.divisionId[i] != 0) {
          this.setState({ divisionId: this.divisionId[i] });
          break;
        }
      }
      const addTemplate = new MarketplaceTemplate();
      addTemplate.id = this.props.fields.length == 1 ? this.props.fields[0]?.id : 0;
      addTemplate.name = fields.name.trim();
      addTemplate.marketplaceId = this.props.selectedMarketPlaceId.key;
      addTemplate.productDepartmentId =
        this.props.selectedMarketPlaceId.key == 1 ? 652 : this.state.productDept;
      addTemplate.divisionId = this.state.divisionId;
      addTemplate.isActive = true;
      try {
        this.setState({ Template: false, fields: { templateName: '' } });
        this.apiClient.addOrUpdateMarketplaceTemplate(addTemplate).then((r: any) => {
          let message =
            r.hasError == false
              ? this.props.fields.length == 0
                ? `${t('MSG_ADD')}`
                : `${t('MSG_UPDATE')}`
              : `${t('ERROR_UNKNOWN')}`;
          this.props.response(r, message, addTemplate.productDepartmentId);
          this.setState({ divisionId: 0 });
          if (r.hasError) {
            this.notify.showErrorNotify(r.message);
          } else {
            this.notify.showSuccessNotify(message);
          }
        });
      } catch (e: any) {
        this.notify.showErrorNotify(e);
      }
    };
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };
  onMultiSelecting(event: any) {
    let data: any = this.state.roleSelected;
    if (event.selected) {
      data.push(event.key);
    } else {
      data = data.filter((value: any) => {
        return value != event.key;
      });
    }
    this.setState({ roleSelected: data });
  }
  render() {
    return (
      <>
        <div className={PIMcontentStyles.header}>
          <Grid container spacing={2}>
            <Grid item xs={10}>
              {this.props.fields.length == 0 ? (
                <span className="apptext1">{`${t('CREATE_TEMPLATE')}`}</span>
              ) : (
                <span className="apptext1">{`${t('EDIT_TEMPLATE')}`}</span>
              )}
            </Grid>
            {/* <Grid item xs={2}>
                            <IconButton
                                styles={iconButtonStyles}
                                iconProps={cancelIcon}
                                onClick={() => this.props.CloseModal()}
                            />
                        </Grid> */}
          </Grid>
        </div>
        <div className={PIMcontentStyles.body}>
          <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1} xs={12} />
              <Grid item lg={10} xs={12}>
                <TextField
                  required
                  className="textField"
                  label={`${t('TEMPLATE_NAME')}`}
                  name="name"
                  placeholder={`${t('TEMPLATE_NAME_PLACEHOLDER')}`}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields.name}
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
            <br />
            {this.props.selectedMarketPlaceId?.key != 1 && (
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={1} xs={12} />
                <Grid item lg={10} xs={12}>
                  <Dropdown
                    required
                    className="dropdown"
                    label={`${t('PRODUCT_DEPARTMENT')}`}
                    options={this.props.departmentOption}
                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                    onChange={(e: any, selectedOption: any) =>
                      this.setState({ productDept: selectedOption.key })
                    }
                    defaultSelectedKey={this.state.productDept}
                  />
                </Grid>
              </Grid>
            )}
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={6} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    type="submit"
                    text={`${t('BTN_SAVE')}`}
                    disabled={
                      this.props.fields.length == 1
                        ? !(this.props.fields[0].name == this.state.fields.name) ||
                          !(this.state.productDept == this.props.fields[0].productDepartmentId)
                          ? false
                          : true
                        : this.state.fields.name == '' ||
                          (this.props.selectedMarketPlaceId?.key != 1 &&
                            this.state.productDept == null)
                        ? true
                        : false
                    }
                  />
                </Grid>
                <Grid item lg={1} xs={12} />
                <Grid item lg={5} xs={12}>
                  <DefaultButton
                    className="button"
                    text={`${t('BTN_CANCEL')}`}
                    styles={PIMfooterButtonCancel}
                    onClick={() => this.props.CloseModal()}
                  />
                </Grid>
              </Grid>
            </div>
          </form>
        </div>
      </>
    );
  }
}
