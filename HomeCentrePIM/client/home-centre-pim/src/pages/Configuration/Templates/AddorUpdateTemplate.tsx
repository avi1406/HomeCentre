import { iconButtonStyles } from "@/common/Button";
import { cancelIcon } from "@/common/properties";
import { Template } from "@/services/ApiClient";
import ApiManager from "@/services/ApiManager";
import { Grid } from "@mui/material";
import ReactFormValidation from "react-form-input-validation";
import { DefaultButton, Dropdown, IconButton } from "office-ui-fabric-react";
import React, { Component } from "react";
import { PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm } from "../../PimStyles";
import AuthManagerService from "@/services/AuthManagerService";
import { t } from "i18next";
import { TextField } from "@fluentui/react";
import NotificationManager from "@/services/NotificationManager";

export default class AddOrUpdateTemplate extends Component<any, any> {
  form: ReactFormValidation;
  authManager = new AuthManagerService();
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();

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
      roleSelected: this.props.selectedRole === undefined ? [] : this.props.selectedRole,
      role: [],
      fields: {
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : null,
        division: this.props.fields?.length == 1 ? this.props.fields[0].division : null,
        isActive: this.props.fields?.length == 1 ? this.props.fields[0].isActive : true,
      },
      divisionId: this.props.fields.length != 0 ? this.props.fields[0].divisionId : 0,
      ArrayData: [],
      Ischeced: this.props.fields[0]?.isActive === true ? true : false,
      isActive1: this.props.fields[0]?.isActive === true ? true : false,
    };

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: 'required',
      division: 'required',
      departmentId: 'required',
      isActive: 'required',
    });
    this.form.onformsubmit = async (fields: any) => {
      this.props.CloseModal();
      const addTemplate = new Template();
      addTemplate.id = this.props.fields.length == 1 ? this.props.fields[0]?.id : 0;
      addTemplate.name = fields.name;
      addTemplate.divisionId = this.state.divisionId;
      addTemplate.roleIds = this.state.roleSelected;
      addTemplate.templateStatusId =
        this.props.fields?.length == 1 ? this.props.fields[0].templateStatusId : 1;
      addTemplate.isActive = this.state.Ischeced;
      try {
        this.setState({ Template: false, fields: { templateName: '' } });
        this.apiClient.addOrUpdateTemplate(addTemplate).then((r: any) => {
          let message =
            r.hasError == false
              ? this.props.fields.length == 0
                ? `${t('MSG_ADD')}`
                : `${t('MSG_UPDATE')}`
              : `${t('ERROR_UNKNOWN')}`;
          this.props.response(r, message);
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
    this.setState(
      (prevState: any) => {
        let data = [...prevState.roleSelected];
        if (event.selected) {
          data.push(event.key);
        } else {
          data = data.filter((value) => value !== event.key);
        }
        return { roleSelected: data };
      },
    );
  }
  areArraysEqual(arr1: any, arr2: any) {
    if (arr1.length !== arr2.length) return false;
    const sortedArr1 = [...arr1].sort();
    const sortedArr2 = [...arr2].sort();
    return sortedArr1.every((element, index) => element === sortedArr2[index]);
  }


  handleCheckboxChange = () => {
    this.setState({
      Ischeced: this.state.Ischeced == false ? true : false,
    });
  };
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
            {/* <Grid item xs={2}> */}
            {/* <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              onClick={() => this.props.CloseModal()}
            /> */}
            {/* </Grid> */}
          </Grid>
        </div>
        <div className={PIMcontentStyles.body}>
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
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1} xs={12} />
              <Grid item lg={10} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  id="division"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('TEMPLATE_ASSIGN_DIVISION')}`}
                  onChange={(e: any, item: any) => this.setState({ divisionId: item.key })}
                  selectedKey={this.state.divisionId}
                  options={this.props.DivisionOption}
                  errorMessage={this.state.errors.division ? this.state.errors.division : ''}
                  styles={PIMdropdownStyles}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1} xs={12} />
              <Grid item lg={10} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  id="role"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('TEMPLATE_ASSIGN_ROLE')}`}
                  defaultSelectedKeys={this.state.roleSelected}
                  options={this.props.roleOption}
                  errorMessage={this.state.errors.role ? this.state.errors.role : ''}
                  styles={PIMdropdownStyles}
                  onChange={(e: any, selectedOption: any) => this.onMultiSelecting(selectedOption)}
                  multiSelect
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
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
                    text={`${t('BTN_SAVE')}`}
                    disabled={
                      this.props.fields.length == 1
                        ? !(this.props.fields[0].name == this.state.fields.name) ||
                          !(this.state.divisionId == this.props.fields[0].divisionId) ||
                          !(this.state.Ischeced == this.props.fields[0]?.isActive) ||
                          !this.areArraysEqual(this.state.roleSelected, this.props.selectedRole)
                          ? false
                          : true
                        : this.state.fields.name == '' ||
                          this.state.divisionId == 0 ||
                          this.state.roleSelected.length == 0
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
                    styles={PIMfooterButtonCancel}
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