import { Studio } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import NotificationManager from '@/services/NotificationManager';
import { DatePicker, Dropdown, TextField } from '@fluentui/react';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import { Text } from '@fluentui/react/lib/Text';
import { DefaultButton, nullRender } from 'office-ui-fabric-react';
import React, { Component } from 'react';
import ReactFormValidation from 'react-form-input-validation';
import {
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonCancel1,
  PIMfooterButtonConfirm,
  PIMfooterButtonConfirm1,
  activeText1,
} from '../../PimStyles';
import AuthManagerService from '@/services/AuthManagerService';

export default class AddorUpdateStudio extends Component<any, any> {
  form: ReactFormValidation;
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  constructor(props: any) {
    super(props);
    this.state = {
      priority1: '',
      studio: this.props.fields?.length == 1 ? this.props.fields[0].studio : null,
      priorityenable: false,
      // disable: this.props.fields?.length == 1 ? true : false,
      ActionValue: {
        dd: this.props.ActionValues,
      },
      capacityError: false,
      fields: {
        id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
        studioName: this.props.fields?.length == 1 ? this.props.fields[0].studioName : null,
        code: this.props.fields?.length == 1 ? this.props.fields[0].code : null,
        city: this.props.fields?.length == 1 ? this.props.fields[0].city : null,
        week1Capacity: this.props.fields?.length == 1 ? this.props.fields[0].week1Capacity : null,
        week2Capacity: this.props.fields?.length == 1 ? this.props.fields[0].week2Capacity : null,
        week3Capacity: this.props.fields?.length == 1 ? this.props.fields[0].week3Capacity : null,
        week4Capacity: this.props.fields?.length == 1 ? this.props.fields[0].week4Capacity : null,
        week5Capacity: this.props.fields?.length == 1 ? this.props.fields[0].week5Capacity : null,
        priority: this.props.fields?.length == 1 ? this.props.fields[0]?.priority : '',
        divisionId: this.props.fields?.length == 1 ? this.props.fields[0].divisionId : null,
        totalCapacity: this.props.fields?.length == 1 ? this.props.fields[0].totalCapacity : null,
        isActive: this.props.fields?.length == 1 ? this.props.fields[0].isActive : true,
        effectiveFromDate:
          this.props.fields?.length == 1 ? this.props.fields[0].effectiveFromDate : null,
      },
      errors: {},
      Ischeced: this.props.fields[0]?.isActive === true ? true : false,
      isActive1: this.props.fields[0]?.isActive === true ? true : false,
      checkInput: false,
      divisionOptionlist: [],
      typeopt1: this.props.fields?.length === 1 ? this.props.fields[0]?.divisionId : null,
      dropdownval: this.props.fields?.length == 1 ? this.props.fields[0].divisionId : null,
      Dropdownselected: false,
       toDate: this.convertToDate(props?.fields[0]?.effectivefromdate),
      proiority12: false,
    };

    this.form = new ReactFormValidation(this);
    this.form.useRules({
      studioName: 'required|regex:/^[a-zA-Z ]*$/',
      code: 'required|regex:/^[0-9]+$/',
      city: 'required',
      week1Capacity: 'required|numeric',
      week2Capacity: 'required|numeric',
      week3Capacity: 'required|numeric',
      week4Capacity: 'required|numeric',
      week5Capacity: 'numeric',
      totalCapacity: '',
      priority: 'required',
      divisionId: 'required',
      isActive: 'required|boolean',
      effectiveFromDate: 'required',
    });
  }

  async componentDidMount() {
    this.apiClient.getDivision().then((r: any) => {
      var divisiondatatype: any = [];
      r.result?.forEach((element: any) => {
        if (this.deptId.includes(element.id)) {
          divisiondatatype.push({
            key: element.id,
            text: element.name,
          });
        }
      });
      this.setState({
        divisionOptionlist: divisiondatatype,
        proiority12: this.props.fields?.length == 1 ? true : false,
      });
    });
  }
  handleDropdownChange = (e: any, item: any) => {
    this.setState({ typeopt1: item.key });
  };
  convertToDate = (dateString:any) => {
    if (!dateString) return null; 
    const parts = dateString.split(/[\s/:-]/);
    const year = parseInt(parts[2], 10);
    const month = parseInt(parts[0], 10) - 1;
    const day = parseInt(parts[1], 10);
    return new Date(year, month, day,0,0,0);
  };
  componentDidUpdate(prevProps: any) {
    if (prevProps.fields !== this.props.fields) {
      const divisionId = this.props.fields?.length === 1 ? this.props.fields[0]?.divisionId : null;
      const selectedDivision = this.state.divisionOptionlist.find(
        (option: any) => option.text === divisionId,
      );

      this.setState({
        typeopt1: selectedDivision ? selectedDivision.key : null,
      });

      if (this.props.fields?.length === 1 && this.state.divisionOptionlist.length === 0) {
        this.apiClient.getDivision().then((r: any) => {
          var divisiondatatype: any = [];
          r.result?.forEach((element: any) => {
            divisiondatatype.push({
              key: element.id,
              text: element.name,
            });
          });
          this.setState({ divisionOptionlist: divisiondatatype });
        });
      }
    }
  }

  handleCheckboxChange = () => {
    var tempIscheced = !this.state.Ischeced;
    this.setState(
      {
        Ischeced: tempIscheced,
      },
      () => {
        this.setState({ checkInput: this.state.Ischeced === this.props.fields[0]?.isActive });
      },
    );
  };

  isSubmitDisabled() {
    const areFieldsEqual = (field1: any, field2: any) => {
      const formattedDateFields = {
        receivedDate: new Date(this.state.toDate).toLocaleDateString(),
      };
      const effectiveFromDate = {
        receivedDate: new Date(field1.effectivefromdate).toLocaleDateString(),
      };
      const result =
        field1.city === field2.city &&
        field1.studioName === field2.studioName &&
        field1.code === field2.code &&
        Number(field1.week1Capacity) === Number(field2.week1Capacity) &&
        Number(field1.week2Capacity) === Number(field2.week2Capacity) &&
        Number(field1.week3Capacity) === Number(field2.week3Capacity) &&
        Number(field1.week4Capacity) === Number(field2.week4Capacity) &&
        Number(field1.week5Capacity) === Number(field2.week5Capacity) &&
        field1.priority === field2.priority &&
        field1.divisionId === this.state.typeopt1 &&
        field1.isActive === this.state.Ischeced &&
        effectiveFromDate.receivedDate === formattedDateFields.receivedDate;
      return result;
    };

    if (this.props.fields.length === 1) {
      const originalFields = this.props.fields[0];
      return (
        Object.values(this.state.errors).some((error) => error !== '') ||
        areFieldsEqual(originalFields, this.state.fields) ||
        this.state.proiority12 === false
      );
    } else {
      return (
        Object.values(this.state.errors).some((error) => error !== '') ||
        this.state.fields.city === null ||
        this.state.fields.city === '' ||
        this.state.fields.studioName === null ||
        this.state.fields.studioName === '' ||
        this.state.fields.code === null ||
        this.state.fields.code === '' ||
        this.state.typeopt1 === null ||
        this.state.fields.week1Capacity === null ||
        this.state.fields.week1Capacity === '' ||
        this.state.fields.week2Capacity === null ||
        this.state.fields.week2Capacity === '' ||
        this.state.fields.week3Capacity === null ||
        this.state.fields.week3Capacity === '' ||
        this.state.fields.week4Capacity === null ||
        this.state.fields.week4Capacity === '' ||
        this.state.fields.priority === null ||
        this.state.fields.priority === '' ||
        this.state.priorityenable === false ||
        this.state.studioenable === false
      );
    }
  }

  saveItem = () => {
    const body = new Studio();
    let checkEdit = true;
    if (this.state.fields.id) {
      body.id = this.state.fields.id;

      const w5C =
        this.state.fields.week5Capacity == null ||
        this.state.fields.week5Capacity == undefined ||
        this.state.fields.week5Capacity == ''
          ? 0
          : parseInt(this.state.fields.week5Capacity);
      const totCap =
        parseInt(this.state.fields.week1Capacity) +
        parseInt(this.state.fields.week2Capacity) +
        parseInt(this.state.fields.week3Capacity) +
        parseInt(this.state.fields.week4Capacity) +
        w5C;

      if (totCap < this.props.fields[0].totalCapacity - this.props.fields[0].remainingCapacity) {
        checkEdit = false;
        this.setState({
          capacityError: true,
        });
        // this.notify.showErrorNotify("TotalCapacity should be greater")
      }
    }

    if (checkEdit) {
      this.setState({
        capacityError: false,
      });
      body.name = this.state.fields.studioName;
      body.code = this.state.fields.code;
      body.city = this.state.fields.city;
      body.week1Capacity = this.state.fields.week1Capacity;
      body.week2Capacity = this.state.fields.week2Capacity;
      body.week3Capacity = this.state.fields.week3Capacity;
      body.week4Capacity = this.state.fields.week4Capacity;
      body.week5Capacity = this.state.fields.week5Capacity;
      body.priority = this.state.fields.priority;
      body.divisionId = this.state.typeopt1;
      body.totalCapacity = 0;
      body.isActive = this.state.Ischeced;
      const adjustedDate = this.state.toDate ? new Date(this.state.toDate) : null;
      let adjustedDateString: string | undefined;
      if (adjustedDate) {
        adjustedDate.setDate(adjustedDate.getDate() + 1);
        adjustedDateString = adjustedDate.toISOString();
      }
      body.effectivefromDate = adjustedDateString;
      const addOrUpdatePromise = this.apiClient.addOrUpdateStudio(body);
      addOrUpdatePromise.then((response: any) => {
        const message = response.hasError
          ? response.message
          : body.id === 0 || body.id === undefined
          ? `${t('MSG_ADD')}`
          : `${t('MSG_UPDATE')}`;
        if (response.hasError) {
          this.notify.showErrorNotify(message);
        } else {
          this.notify.showSuccessNotify(message);
          this.props.dataRefresh();
          this.props.clearSearch();
          this.props.ClosePopup();
        }
      });
    }
  };

  Callme = () => {
    if (this.state.toDate !== undefined) {
      const timestampToDate = (date: any) => (date ? date.getTime() : null);
      if (timestampToDate(this.state.toDate1) !== timestampToDate(this.state.toDate)) {
        this.setState({ dateval: true });
      } else {
        this.setState({ dateval: false });
      }
    } else {
      this.setState({ dropdownval: false });
    }

    if (this.state.typeopt !== undefined && this.props.fields?.[0] !== undefined) {
      if (this.state.typeopt !== this.props.fields[0].imageType) {
        this.setState({ dropdownval: true });
      } else {
        this.setState({ dropdownval: false });
      }
    }
  };
  Dropdown = () => {
    if (this.state.typeopt1 !== undefined) {
      let Checkstudio;
      let Checkpriority: any;
      let ValidPriority: any;
      let ValidPriorityemplty: any;
      if (this.state.typeopt1 != null) {
        let isValid;
        if (this.state.fields.studioName != null || this.state.fields.studioName != '') {
          isValid = /^[a-zA-Z ]*$/.test(this.state.fields.studioName);
          Checkstudio = this.props.studio.some(
            (studio: any) =>
              studio.name === this.state.fields.studioName &&
              studio.divisionId === this.state.typeopt1,
          );
        }

        if (this.state.fields.priority != null || this.state.fields.priority != '') {
          ValidPriority = /^P\d+$/.test(this.state.fields.priority);
          ValidPriorityemplty = this.state.fields.priority.trim() === '';
          Checkpriority = this.props.studio.some(
            (studio: any) =>
              studio.priority === this.state.fields.priority &&
              studio.divisionId === this.state.typeopt1,
          );
        }
        if (Checkstudio == false) {
          this.setState({ studioenable: true });
        } else {
          this.setState({ studioenable: false });
        }
        if (Checkpriority == false) {
          this.setState({ priorityenable: true });
        } else {
          this.setState({ priorityenable: false });
        }
        if (!isValid) {
          this.setState((prevState: any) => ({
            errors: {
              ...prevState.errors,
              studioName: 'Studio name should contain only letters.',
            },
          }));
        } else if (Checkstudio) {
          this.setState((prevState: any) => ({
            errors: {
              ...prevState.errors,
              studioName: 'Studio name already exists By using this Division.',
            },
          }));
        } else {
          this.setState((prevState: any) => ({
            errors: {
              ...prevState.errors,
              studioName: '',
            },
          }));
        }
        this.setState((prevState: any) => ({
          errors: {
            ...prevState.errors,
            priority: ValidPriorityemplty
              ? 'The priority field is required.'
              : ValidPriority
              ? Checkpriority
                ? 'Priority name already exists by using this Division.'
                : ''
              : "Value should start with 'P' and be followed by numbers.",
          },
        }));
        if (this.state.dropdownval == this.state.typeopt1) {
          this.setState({ Dropdownselected: false });
        } else {
          this.setState({ Dropdownselected: true });
        }

        const currentStudioCode = this.state.fields.code;
        if (currentStudioCode) {
          this.checkStudioCodeExistence(currentStudioCode);
        }
      }
    }
  };

  checkStudioNameExistence(studioName: any) {
    const isValid = /^[a-zA-Z ]*$/.test(studioName);
    let studioExists = this.props.studio.some((studio: any) => studio.name === studioName);
    let Checkstudio;
    if (this.state.typeopt1 != null) {
      Checkstudio = this.props.studio.some(
        (studio: any) => studio.name === studioName && studio.divisionId === this.state.typeopt1,
      );
    }
    if (Checkstudio === false) {
      studioExists = false;
    }
    if (!isValid) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          studioName: 'Studio name should contain only letters.',
        },
      }));
    } else if (studioExists) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          studioName: 'Studio name already exists.',
        },
      }));
    } else {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          studioName: '',
        },
      }));
    }
    this.setState({ studioenable: isValid && !studioExists });
    
    // const currentStudioCode = this.state.fields.code;
    // if (currentStudioCode) {
    //   this.checkStudioCodeExistence(currentStudioCode);
    // }
  }

  checkStudioCodeExistence(code: any) {
    const isValid = /^[0-9]+$/.test(code);
    let studioExists = this.props.studio.some((studio: any) => studio.code == code);
    let Checkstudio;
    
    if (this.state.typeopt1 != null) {
      Checkstudio = this.props.studio.some(
        (studio: any) => studio.code == code,
      );
    }
  
    if (Checkstudio === false) {
      studioExists = false;
    }
  
    if (!isValid) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          code: 'Studio code should be a number.',
        },
      }));
    } else
     if (studioExists) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          code: 'Studio code should be unique.',
        },
      }));
    } else {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          code: '',
        },
      }));
    }
    this.setState({ studioenable: isValid && !studioExists });
  }
  
  checkPriorityExistence(priority: any) {
    const isValid = /^P\d+$/.test(priority);
    const isRequiredError = priority.trim() === '';
    let priorityExists = this.props.studio.some((studio: any) => studio.priority == priority);
    let Checkstudio;

    if (this.state.typeopt1 != null) {
      Checkstudio = this.props.studio.some(
        (studio: any) => studio.priority === priority && studio.divisionId === this.state.typeopt1,
      );
    }
    if (Checkstudio === false) {
      priorityExists = false;
    }

    if (isRequiredError) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          priority: 'The priority field is required.',
        },
      }));
    } else if (!isValid) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          priority: "Value should start with 'P' and be followed by numbers.",
        },
      }));
    } else if (priorityExists) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          priority: 'Priority name already exists.',
        },
      }));
    } else if (Checkstudio) {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          studioName: 'Studio name already exists By using this Division.',
        },
      }));
    } else {
      this.setState((prevState: any) => ({
        errors: {
          ...prevState.errors,
          priority: '',
        },
      }));
    }

    this.setState({
      priority1: priority,
      priorityenable: isValid && !priorityExists,
      proiority12: !priorityExists,
    });
  }

  render() {
    var date = this.state.toDate != null ? this.state.toDate : null;
    let isEffectiveToday;
    if (date != null && date != undefined) {
      const today = new Date();
      const formattedToday = {
        receivedDate: new Date(today).toLocaleDateString(),
      };
      const effectiveFromDate = {
        receivedDate: new Date(this.state.toDate).toLocaleDateString(),
      };
      isEffectiveToday =
        new Date(effectiveFromDate.receivedDate) <= new Date(formattedToday.receivedDate);
    } else {
      isEffectiveToday = false;
    }
    return (
      <>
        <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
          {(this.props.fields?.length === 1 ? true : false) && (
            <Grid item lg={10} xs={12}>
              {isEffectiveToday && (
                <div className={PIMcontentStyles.attributeBodyStudioActive}>
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
              )}
            </Grid>
          )}
          <div className={PIMcontentStyles.attributeBody}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <Dropdown
                  required
                  className="dropdown"
                  placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                  label={`${t('DIVISION')}`}
                  options={this.state.divisionOptionlist}
                  selectedKey={this.state.typeopt1}
                  errorMessage={this.state.errors.divisionId ? this.state.errors.divisionId : ''}
                  disabled={
                    this.state.typeopt1 &&
                    isEffectiveToday &&
                    this.state.disable &&
                    this.props.fields.length == 1
                      ? this.props.fields[0].isActive == true
                        ? true
                        : false
                      : false
                  }
                  onChange={(e: any, item: any) => {
                    this.setState({ typeopt: item.text, typeopt1: item.key }, () => {
                      this.Dropdown();
                    }),
                      this.form.handleChangeEvent(e);
                  }}
                  styles={PIMdropdownStyles}
                />
                {this.props.fields?.length !== 1 && (
                  <span className="selectDivisionWarning">
                    {!this.state.typeopt ? 'Select Division.' : ''}
                  </span>
                )}
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <Text className="effectiveFromText">{t('COL_EFFECTIVE_FROM')}</Text>
                <DatePicker
                  className="dateEffect"
                  showMonthPickerAsOverlay={true}
                  formatDate={(e: any) => e.toLocaleDateString('en-GB')}
                  // minDate={this.state.minDate}
                  // maxDate={new Date()}
                  minDate={new Date()}
                  onSelectDate={(date) => this.setState({ toDate: date })}
                  value={this.state.toDate ? new Date(this.state.toDate) : undefined}
                  showGoToToday={true}
                  disabled={this.state.toDate && isEffectiveToday && this.state.disable}
                />
              </Grid>
              {/* <Grid item lg={0.95} xs={12} /> */}
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="studioName"
                  required
                  className="textField"
                  label={`${t('COL_STUDIO_NAME')}`}
                  value={this.state.fields.studioName}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (value != '') {
                      this.checkStudioNameExistence(value);
                    }
                    this.form.handleChangeEvent(event);
                  }}
                  errorMessage={this.state.errors.studioName ? this.state.errors.studioName : ''}
                  disabled={this.state.fields.studioName && isEffectiveToday && this.state.disable}
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="code"
                  required
                  className="textField"
                  label={`${t('COL_STUDIO_CODE')}`}
                  value={this.state.fields.code}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                  const value = event.target.value;
                  if (value !== '') {
                    this.checkStudioCodeExistence(value);
                  }
                  this.form.handleChangeEvent(event);
                }}
                errorMessage={this.state.errors.code ? this.state.errors.code : ''}
                disabled={this.state.fields.code && this.state.disable}
              />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>
            <br />

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="city"
                  required
                  className="textField"
                  label={`${t('COL_CITY')}`}
                  value={this.state.fields.city}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  errorMessage={this.state.errors.city ? this.state.errors.city : ''}
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="week1Capacity"
                  required
                  className="textField"
                  label={`${t('COL_WEEK_1')}`}
                  value={this.state.fields.week1Capacity}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (!isNaN(value)) {
                      this.form.handleChangeEvent(event);
                    } else {
                      this.setState({
                        errors: {
                          ...this.state.errors,
                          week1Capacity: 'Please enter a valid number.',
                        },
                      });
                    }
                  }}
                  errorMessage={
                    this.state.errors.week1Capacity ? this.state.errors.week1Capacity : ''
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="week2Capacity"
                  required
                  className="textField"
                  label={`${t('COL_WEEK_2')}`}
                  value={this.state.fields.week2Capacity}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (!isNaN(value)) {
                      this.form.handleChangeEvent(event);
                    } else {
                      this.setState({
                        errors: {
                          ...this.state.errors,
                          week2Capacity: 'Please enter a valid number.',
                        },
                      });
                    }
                  }}
                  errorMessage={
                    this.state.errors.week2Capacity ? this.state.errors.week2Capacity : ''
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="week3Capacity"
                  required
                  className="textField"
                  label={`${t('COL_WEEK_3')}`}
                  value={this.state.fields.week3Capacity}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (!isNaN(value)) {
                      this.form.handleChangeEvent(event);
                    } else {
                      this.setState({
                        errors: {
                          ...this.state.errors,
                          week3Capacity: 'Please enter a valid number.',
                        },
                      });
                    }
                  }}
                  errorMessage={
                    this.state.errors.week3Capacity ? this.state.errors.week3Capacity : ''
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="week4Capacity"
                  required
                  className="textField"
                  label={`${t('COL_WEEK_4')}`}
                  value={this.state.fields.week4Capacity}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (!isNaN(value)) {
                      this.form.handleChangeEvent(event);
                    } else {
                      this.setState({
                        errors: {
                          ...this.state.errors,
                          week4Capacity: 'Please enter a valid number.',
                        },
                      });
                    }
                  }}
                  errorMessage={
                    this.state.errors.week4Capacity ? this.state.errors.week4Capacity : ''
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="week5Capacity"
                  className="textField"
                  label={`${t('COL_WEEK_5')}`}
                  value={this.state.fields.week5Capacity}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    const value = event.target.value;
                    if (!isNaN(value)) {
                      this.form.handleChangeEvent(event);
                    } else {
                      this.setState({
                        errors: {
                          ...this.state.errors,
                          week5Capacity: 'Please enter a valid number.',
                        },
                      });
                    }
                  }}
                  errorMessage={
                    this.state.errors.week5Capacity ? this.state.errors.week5Capacity : ''
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>

            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="totalCapacity"
                  className="textField"
                  label={`${t('COL_TOTAL_CAPACITY')}`}
                  value={(
                    parseInt(this.state.fields.week1Capacity) +
                    parseInt(this.state.fields.week2Capacity) +
                    parseInt(this.state.fields.week3Capacity) +
                    parseInt(this.state.fields.week4Capacity) +
                    (this.state.fields.week5Capacity == null ||
                    this.state.fields.week5Capacity == undefined ||
                    this.state.fields.week5Capacity == ''
                      ? 0
                      : parseInt(this.state.fields.week5Capacity))
                  ).toString()}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  disabled={true}
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="priority"
                  required
                  className="textField"
                  label={`${t('PRIORITY')}`}
                  value={this.state.fields.priority}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => {
                    this.form.handleChangeEvent(event);
                    const value = event.target.value;
                    this.checkPriorityExistence(value);
                  }}
                  errorMessage={this.state.errors.priority || ''}
                />
              </Grid>
            </Grid>

            <br />
          </div>
          {this.state.capacityError && (
            <span className="capacity-error">
              Total capcity should be greater than or equal to{' '}
              {this.props.fields[0].totalCapacity - this.props.fields[0].remainingCapacity}
            </span>
          )}
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={
                    this.state.fields.id === 0 ? PIMfooterButtonConfirm : PIMfooterButtonConfirm1
                  }
                  type="submit"
                  text={this.state.fields.id === 0 ? `${t('BTN_SAVE')}` : `${t('EDIT_BTN_SUBMIT')}`}
                  onClick={this.saveItem}
                  disabled={this.isSubmitDisabled()}
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
