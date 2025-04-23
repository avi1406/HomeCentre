import ApiManager from '@/services/ApiManager';
import NotificationManager from '@/services/NotificationManager';
import { Dropdown, IDropdownOption } from '@fluentui/react';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import { DefaultButton } from 'office-ui-fabric-react';
import React from 'react';
import ReactFormValidation from 'react-form-input-validation';
import {
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonCancel1,
  PIMfooterButtonConfirm,
} from '../PimStyles';

class UpdateItemCode extends React.Component<any, any> {
  form: ReactFormValidation;
  statusOptions: any = [
    { key: 'P0', text: 'P0' },
    { key: 'P1', text: 'P1' },
  ];
  notify = new NotificationManager();
  // apiClient: any;
  apiClient = new ApiManager().CreateApiClient();
  constructor(props: any) {
    super(props);
    this.state = {
      fields: {
        itemcode: this.props.fields?.length === 1 ? this.props.fields[0].itemCode : 0,
        name: this.props.fields?.length === 1 ? this.props.fields[0].StudioName : null,
        priority: this.props.fields?.length === 1 ? this.props.fields[0].StudioPriority : null,
        city: this.props.fields?.length === 1 ? this.props.fields[0].StudioCity : null,
      },
      errors: {},
      checkInput: false,
      pirority: null,
      name: null,
      city: null,
      typeopt1: this.props.fields?.length === 1 ? this.props.fields[0]?.StudioCity : null,
      typeopt2: this.props.fields?.length === 1 ? this.props.fields[0]?.StudioName : null,
      typeopt: this.props.fields?.length === 1 ? this.props.fields[0].StudioName : null,
      // typeopt3: this.props.fields?.length === 1 ? this.props.fields[0]?.StudioPriority : null,
      typeopt3: this.getTypeOpt3Value(this.props.fields[0]),
      Dropdownselected: false,
      Dropdownselected1: false,
      Dropdownselected2: false,
      studio: [],
    };

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      priority: 'required',
      name: 'required',
      city: 'required',
      itemcode: 'required',
    });
  }
  getTypeOpt3Value(field: any) {
    if (!field || !field.StudioName) {
      if (field && field.imageType === 'GRN') {
        return 'P0';
      } else {
        return 'P1';
      }
    } else {
      if (field && field.StudioPriority) {
        return field.StudioPriority;
      } else {
        return null; 
      }
    }
  }
  handleCheckboxChange = () => {
    this.setState({
      Ischeced: !this.state.Ischeced,
      checkInput: this.state.Ischeced === this.props.fields[0]?.isActive,
    });
  };
  ValueChecked = () => {};
  componentDidMount(): void {
    const dropdownData = this.props.studio.map((studio: any) => ({
      name: studio.name,
      city: studio.city,
      priority: studio.priority,
    }));
    this.setState({ studio: dropdownData });
  }
  Dropdown = () => {
    if (this.state.typeopt1 !== undefined) {
      if (this.state.dropdownval == this.state.typeopt1) {
        this.setState({ Dropdownselected: false });
      } else {
        this.setState({ Dropdownselected: true });
      }
    }
    if (this.state.typeopt2 !== undefined) {
      if (this.state.dropdownval == this.state.typeopt2) {
        this.setState({ Dropdownselected1: false });
      } else {
        this.setState({ Dropdownselected1: true });
      }
    }
    if (this.state.typeopt3 !== undefined) {
      if (this.state.dropdownval == this.state.typeopt3) {
        this.setState({ Dropdownselected2: false });
      } else {
        this.setState({ Dropdownselected2: true });
      }
    }
  };
  saveItem = () => {
    var city = this.state.typeopt1;
    var name = this.state.typeopt2;
    var priority = this.state.typeopt3;
    var itemCode = this.props.fields[0].itemCode;
    this.apiClient.updateCAMQCRecords(city, name, priority, itemCode).then((response: any) => {
      let message = response.hasError ? response.message : `${t('MSG_UPDATE')}`;
      if (response.hasError) {
        this.notify.showErrorNotify(message);
      } else {
        this.notify.showSuccessNotify(message);
      }
      this.props.dataRefresh();
      this.props.ClosePopup();
    });
  };

  updateOtherDropdowns = (selectedName: string) => {
    const selectedStudio = this.props.studio.find((studio: any) => studio.name === selectedName);
    this.setState(
      {
        typeopt1: selectedStudio ? selectedStudio.city : null,
        // typeopt3: selectedStudio ? selectedStudio.priority : null,
      },
      () => {},
    );
  };
  componentDidUpdate(prevProps: any) {
    if (prevProps.fields !== this.props.fields) {
      const city = this.props.fields?.length === 1 ? this.props.fields[0]?.city : null;
      const selectedDivision = this.state.locationoption.find(
        (option: any) => option.text === city,
      );
      this.setState({
        typeopt1: selectedDivision ? selectedDivision.key : null,
      });
    } else if (prevProps.fields !== this.props.fields) {
      const name = this.props.fields?.length === 1 ? this.props.fields[0]?.StudioName : null;
      const selectedDivision2 = this.state.nameoption.find((option: any) => option.text === name);
      this.setState({
        typeopt2: selectedDivision2 ? selectedDivision2.key : null,
      });
    } else if (prevProps.fields !== this.props.fields) {
      const priority = this.props.fields?.length === 1 ? this.props.fields[0]?.priority : null;
      const selectedDivision3 = this.state.pirorityoption.find(
        (option: any) => option.text === priority,
      );
      this.setState({
        typeopt3: selectedDivision3 ? selectedDivision3.key : null,
      });
    }
  }
  // Ischanged = () => {
  //   if (
  //     this.state.typeopt != undefined &&
  //     this.state.typeopt != null &&
  //     this.state.typeopt3 != undefined &&
  //     this.state.typeopt3 != null
  //   ) {
  //     return (
  //       this.props.fields[0]?.name === this.state.typeopt &&
  //       this.props.fields[0]?.StudioPriority == this.state.typeopt3
  //     );
  //   } else {
  //     return true;
  //   }
  // };

  Ischanged = () => {
    if (
      this.state.typeopt !== undefined &&
      this.state.typeopt !== null &&
      this.state.typeopt3 !== undefined &&
      this.state.typeopt3 !== null
    ) {
      if (
        this.props.fields[0]?.StudioName == this.state.typeopt &&
        this.props.fields[0]?.StudioPriority == this.state.typeopt3
      ) {
        return true;
      } else {
        return false;
      }
    } else {
      return true;
    }
  };

  render() {
    return (
      <>
        <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
          <div className={PIMcontentStyles.attributeBody}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12} />
              <Grid item lg={8} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('COL_STUDIO_NAME')}`}
                  selectedKey={this.state.typeopt2}
                  options={this.props.studio.map((studio: any) => ({
                    key: studio.name,
                    text: studio.name,
                  }))}
                  errorMessage={this.state.errors.nameoption ? this.state.errors.nameoption : ''}
                  onChange={(e: any, item: any) => {
                    this.setState({ typeopt2: item.key, typeopt: item.text }, () => {
                      this.updateOtherDropdowns(item.text);
                    });
                  }}
                  styles={PIMdropdownStyles}
                />
              </Grid>
            </Grid>

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12} />
              <Grid item lg={8} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('COL_STUDIO_LOCATION')}`}
                  // options={this.props.locationoption}
                  options={this.props.studio.map((studio: any) => ({
                    key: studio.city,
                    text: studio.city,
                  }))}
                  selectedKey={this.state.typeopt1}
                  errorMessage={
                    this.state.errors.locationoption ? this.state.errors.locationoption : ''
                  }
                  // onChange={(e: any, item: any) => {
                  //   this.setState({ typeopt: item.text, typeopt1: item.key }, () => {
                  //     this.Dropdown();
                  //   });
                  // }}
                  disabled={true}
                  styles={PIMdropdownStyles}
                />
              </Grid>
            </Grid>

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12} />
              <Grid item lg={8} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('PRIORITY')}`}
                  selectedKey={this.state.typeopt3}
                  options={this.statusOptions}
                  // options={this.props.studio.map((studio: any) => ({
                  //   key: studio.priority,
                  //   text: studio.priority,
                  // }))}
                  onChanged={(selectedOption: IDropdownOption) => {
                    this.setState({ typeopt3: selectedOption.key });
                  }}
                  errorMessage={
                    this.state.errors.pirorityoption ? this.state.errors.pirorityoption : ''
                  }
                  // onChange={(e: any, item: any) => {
                  //   this.setState({ typeopt: item.text, typeopt3: item.key }, () => {
                  //     this.Dropdown();
                  //   });
                  // }}
                  // disabled={true}
                  styles={PIMdropdownStyles}
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
                  className="button ic_align_btn"
                  styles={PIMfooterButtonConfirm}
                  type="submit"
                  text={`${t('BTN_SAVE')}`}
                  disabled={this.Ischanged()}
                  onClick={this.saveItem}
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
                  onClick={this.props.ClosePopup}
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

export default UpdateItemCode;
