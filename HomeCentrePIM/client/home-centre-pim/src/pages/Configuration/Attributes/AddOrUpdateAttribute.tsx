import { ProductAttribute } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import NotificationManager from '@/services/NotificationManager';
import { Dropdown, TextField } from '@fluentui/react';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import { DefaultButton } from 'office-ui-fabric-react';
import React, { Component } from 'react';
import ReactFormValidation from 'react-form-input-validation';
import {
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonCancel1,
  PIMfooterButtonConfirm,
} from '../../PimStyles';

export default class AddOrUpdateAttribute extends Component<any, any> {
  form: ReactFormValidation;
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  fieldType: any = [
    { key: true, text: t('MANDATORY') },
    { key: false, text: t('OPTIONAL') },
  ];

  constructor(props: any) {
    super(props);
    this.state = {
      lookUpdata: [],
      lookUpColumndata: [],
      fields: {
        id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : '',
        isActive: this.props.fields?.length == 1 ? this.props.fields[0].isActive : true,
      },
      fieldType: this.props.fields?.length == 1 ? this.props.fields[0].isRequired : null,
      division: this.props.fields?.length == 1 ? this.props.fields[0].divisionId : null,
      dataType: this.props.fields?.length == 1 ? this.props.fields[0].dataType : null,
      lookUpColumn: this.props.fields?.length == 1 ? this.props.fields[0]?.lookupTableColumn : '',
      lookUpTable: this.props.fields?.length == 1 ? this.props.fields[0]?.lookupTable : '',
      errors: {},
      Ischeced: this.props.fields[0]?.isActive === true ? true : false,
      isActive1: this.props.fields[0]?.isActive === true ? true : false,
      checkInput: false,
    };

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: 'required',
      dataType: 'required',
      fieldType: 'required',
      division: 'required',
      isActive: 'required|boolean',
    });

    this.form.onformsubmit = async (fields) => {
      const state = this.state;
      const lookupTable =
        state.lookUpTable == '' || state.lookUpTable == undefined ? '' : state.lookUpTable?.text;
      const lookupcolumn =
        this.state.lookUpColumn == '' || this.state.lookUpColumn == undefined
          ? ''
          : this.state.lookUpColumn?.text;
      this.props.ClosePopup();
      var body = new ProductAttribute();
      body.id = fields.id;
      body.name = fields.name;
      body.dataType = this.state.dataType;
      body.isRequired = this.state.fieldType;
      body.label = fields.name;
      body.divisionId = this.state.division;
      body.lookupTable = this.state.dataType == 30 ? lookupTable : '';
      body.lookupTableColumn = this.state.dataType == 30 ? lookupcolumn : '';
      if (this.state.checkInput) {
        body.isActive = this.state.Ischeced;
      } else {
        body.isActive = true;
      }
      this.apiClient.addOrUpdateAttribute(body).then((response: any) => {
        let message = response.hasError
          ? response.message
          : fields.id == 0
          ? `${t('MSG_ADD')}`
          : `${t('MSG_UPDATE')}`;
        if (response.hasError) {
          this.notify.showErrorNotify(message);
        } else {
          this.notify.showSuccessNotify(message);
          this.props.dataRefresh();
        }
      });
    };
  }

  // async componentDidMount() {
  //   var data: any = [];
  //   var colData: any = [];
  //   const response = (await this.apiClient.getTables()).result;
  //   const colResponse = (await this.apiClient.getTableColumns()).result;
  //   response?.forEach((element: any) => {
  //     data.push({ key: element.id, text: element.name });
  //   });
  //   colResponse?.forEach((e: any) => {
  //     colData.push({ key: e.id, text: e.name });
  //   });
  //   var props = this.props;
  //   this.setState({
  //     lookUpdata: data,
  //     lookUpColumndata: colData,
  //     lookUpTable:
  //       this.props.fields?.length == 1
  //         ? data.filter((element: any) => element.text == props.fields[0]?.lookupTable)[0]
  //         : '',
  //     lookUpColumn:
  //       this.props.fields?.length == 1
  //         ? colData.filter((element: any) => element.text == props.fields[0]?.lookupTableColumn)[0]
  //         : '',
  //   });
  // }
  async componentDidMount(): Promise<void> {
    try {
      // Fetch tables and columns concurrently
      const [tableResponse, colResponse] = await Promise.all([
        this.apiClient.getTables(),
        this.apiClient.getTableColumns(),
      ]);
  
      // Process table data
      const data = (tableResponse?.result || []).map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      // Process column data
      const colData = (colResponse?.result || []).map((e: any) => ({
        key: e.id,
        text: e.name,
      }));
  
      // Prepare props for further processing
      const props = this.props;
  
      // Set lookup data and determine the selected table and column
      const lookUpTable =
        props.fields?.length === 1
          ? data.find((element: any) => element.text === props.fields[0]?.lookupTable) || null
          : null;
  
      const lookUpColumn =
        props.fields?.length === 1
          ? colData.find((element: any) => element.text === props.fields[0]?.lookupTableColumn) || null
          : null;
  
      // Set the state with processed data
      this.setState({
        lookUpdata: data,
        lookUpColumndata: colData,
        lookUpTable,
        lookUpColumn,
      });
    } catch (e: any) {
      // Handle any errors during the data fetching process
      this.notify.showErrorNotify(e.message);
    }
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
        <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
          {(this.props.fields?.length === 1 ? true : false) && (
            <Grid item lg={10} xs={12}>
              <div className={PIMcontentStyles.attributeBody12}>
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
          <div className={PIMcontentStyles.attributeBody}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="name"
                  required
                  className="textField"
                  label={`${t('ATTRIBUTE_NAME')}`}
                  placeholder={`${t('ATTRIBUTE_NAME_PLACEHOLDER')}`}
                  value={this.state.fields.name}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  errorMessage={this.state.errors.name ? this.state.errors.name : ''}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <Dropdown
                  className="dropdown"
                  required
                  id="dataType"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('ATTRIBUTE_DATA_TYPE')}`}
                  onChange={(e: any, item: any) => this.setState({ dataType: item.key })}
                  selectedKey={this.state.dataType}
                  options={this.props.dataType}
                  errorMessage={this.state.errors.dataType ? this.state.errors.dataType : ''}
                  styles={PIMdropdownStyles}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>
            <br />
            {this.state.dataType == 30 && (
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={0.85} xs={12} />
                <Grid item lg={4.5} xs={12}>
                  <Dropdown
                    className="dropdown"
                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                    label={`${t('LOOK_UP_TABLE')}`}
                    options={this.state.lookUpdata}
                    selectedKey={this.state.lookUpTable?.key}
                    onChange={(e: any, item: any) => this.setState({ lookUpTable: item })}
                    styles={PIMdropdownStyles}
                    disabled={
                      !(this.state.isActive1 === true
                        ? true
                        : false || (this.props.fields?.length === 1 ? false : true))
                    }
                  />
                </Grid>
                <Grid item lg={0.95} xs={12} />
                <Grid item lg={4.5} xs={12}>
                  <Dropdown
                    className="dropdown"
                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                    label={`${t('LOOK_UP_COLUMN')}`}
                    options={this.state.lookUpColumndata}
                    onChange={(e: any, item: any) => this.setState({ lookUpColumn: item })}
                    selectedKey={this.state.lookUpColumn?.key}
                    styles={PIMdropdownStyles}
                    disabled={
                      !(this.state.isActive1 === true
                        ? true
                        : false || (this.props.fields?.length === 1 ? false : true))
                    }
                  />
                </Grid>
              </Grid>
            )}
            {this.state.dataType == 30 && <br />}
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('ATTRIBUTE_FIELD_TYPE')}`}
                  selectedKey={this.state.fieldType}
                  options={this.fieldType}
                  errorMessage={this.state.errors.fieldType ? this.state.errors.fieldType : ''}
                  onChange={(e: any, item: any) => this.setState({ fieldType: item.key })}
                  styles={PIMdropdownStyles}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('DIVISION')}`}
                  defaultSelectedKey={this.state.division}
                  selectedKey={this.state.division}
                  options={this.props.division}
                  errorMessage={this.state.errors.division ? this.state.errors.division : ''}
                  onChange={(e: any, item: any) => this.setState({ division: item.key })}
                  styles={PIMdropdownStyles}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
            </Grid>
            <br />
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  type="submit"
                  text={this.state.fields.id == 0 ? `${t('BTN_SAVE')}` : `${t('EDIT_BTN_SUBMIT')}`}
                  disabled={
                    this.props.fields.length == 1
                      ? !(this.props.fields[0]?.name == this.state.fields.name) ||
                        !(this.state.fieldType == this.props.fields[0]?.isRequired) ||
                        !(this.state.dataType == this.props.fields[0]?.dataType) ||
                        !(this.state.division == this.props.fields[0]?.divisionId) ||
                        !(this.state.lookUpTable?.text == this.props.fields[0]?.lookUpTable) ||
                        !(this.state.lookUpColumn?.text == this.props.fields[0]?.lookUpColumn) ||
                        !(this.state.Ischeced === this.props.fields[0]?.isActive)
                        ? false
                        : true
                      : this.state.fields.name == '' ||
                        this.state.dataType == null ||
                        this.state.fieldType == null ||
                        this.state.division == null
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
                    this.props.fields?.length === 1 ? PIMfooterButtonCancel1 : PIMfooterButtonCancel
                  }
                  onClick={() => this.props.ClosePopup()}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </form>
      </>
    );
  }
}
