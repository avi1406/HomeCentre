import { departmentSelect, textField } from '@/common/properties';
import ApiManager from '@/services/ApiManager';
import { DefaultButton, IChoiceGroupOption, TextField } from '@fluentui/react';
import React, { Suspense } from 'react';
import ReactFormValidation from 'react-form-input-validation';
import { Dropdown } from '@fluentui/react/lib/Dropdown';
import { uidCreate } from '@/common/Button';
import { UidInitiation, UserDepartment } from '@/services/ApiClient';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import { Grid } from '@mui/material';
import NotificationManager from '@/services/NotificationManager';

class FormPlanogramOrFamily extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  // form: ReactFormValidation;
  dicriptionLimitValue: any;
  createPlanoLimitValue: any;
  constructor(props: any) {
    super(props);
    this.state = {
      divisionOptionlst: [],
      divisionFullList: [],
      divisionSelectedKey: '',
      divisionIsPlanoGram: false,
      errors: {},
      dataList: '',
      planoOrFamilynameList: [],
      suggestions: [],
      description: '',
      familyname: '',
      planogramname: '',
      planOrFamilynameLength: 0,
      showPlanogramFields: true,
    };

    // this.form = new ReactFormValidation(this);

    // this.form.
    //   onformsubmit = async (fields: any) => {
    //       debugger
    //     this.setState({ suggestions: [], planoOrFamilynameList: [] });
    //     try {
    //       const planogramName = fields.planogramName?.trim().replace(/\s+/g, ' ');
    //       const familyName = fields.familyName?.trim().replace(/\s+/g, ' ');
    //       const description = fields.description?.trim().replace(/\s+/g, ' ');
    //       var departmentData: UserDepartment = this.state.divisionFullList?.find(
    //         (a: UserDepartment) => a.divisionId === this.state.divisionSelectedKey,
    //       );
    //       var addplano = new UidInitiation();
    //       addplano.description = description || '';
    //       addplano.planogram = planogramName || '';
    //       addplano.familyName = familyName || '';
    //       addplano.departmentId =
    //         departmentData?.departmentId == undefined
    //           ? this.state.divisionOptionlst[0].key
    //           : departmentData?.departmentId;
    //       addplano.divisionId =
    //         departmentData?.divisionId == undefined
    //           ? this.state.divisionOptionlst[0].key
    //           : departmentData?.departmentId;
    //       this.apiClient.getIfAlreadyExistorNot(planogramName || familyName).then((r: any) => {
    //         const ifExist = r.result;
    //         if (ifExist == false) {
    //           this.apiClient.addPlanogramOrFamilyName(addplano).then((r: any) => {
    //             if (r.hasError) {
    //               this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    //             } else {
    //               this.notify.showSuccessNotify(`${this.props.t('MSG-ADDED_SUCCESSFULLY')}`);
    //             }
    //             (error: any) => {
    //               this.notify.showErrorNotify(error);
    //             };
    //             this.setState({ fields: { description: '', planogramName: '', familyName: '' } });
    //             this.getDepartmentList();
    //           });
    //         } else {
    //           this.setState({
    //             errors: {
    //               familyName: `${fields.planogramName || fields.familyName} is already exist`,
    //               planogramName: `${fields.planogramName || fields.familyName} is already exist`,
    //             },
    //           });
    //         }
    //       });
    //     } catch (e: any) {
    //       this.notify.showErrorNotify(e.notify);
    //       this.setState({ familyname: '', planogramname: '' });
    //     }
    //   };
  }
  async componentDidMount() {
    this.getDepartmentList().then((r) => r);
    // const configValue: any = await this.apiClient.getAppConfig();
    // const dicriptionConfiguration = parseInt(
    //   configValue?.result?.find((e: any) => e.name == 'PlanogramDiscriptionLength')?.value,
    // );
    // const createPlanoConfiguration = parseInt(
    //   configValue?.result?.find((e: any) => e.name == 'CreatePlanogram')?.value,
    // );
    // this.createPlanoLimitValue = createPlanoConfiguration;
    // this.dicriptionLimitValue = dicriptionConfiguration;
    // this.setState(
    //   {
    //     errors: {
    //       planogramName: '',
    //       familyName: '',
    //       description: '',
    //     },
    //   },
    //   () => {
    //     this.form.useRules({
    //       familyName: `required|regex:/^[a-zA-Z0-9\\s]*[a-zA-Z][a-zA-Z0-9\\s]*$/|max:${this.createPlanoLimitValue}`,
    //       planogramName: `required|regex:/^[a-zA-Z0-9\\s]*[a-zA-Z][a-zA-Z0-9\\s]*$/|max:${this.createPlanoLimitValue}`,
    //       description: `required|string|max:${this.dicriptionLimitValue}`,
    //       type: 'required',
    //       department: '',
    //     });
    //   },
    // );
  }
  handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    this.setState({ suggestions: [], planoOrFamilynameList: [] });

    try {
      const { planogramname, familyname, description } = this.state;

      var departmentData: UserDepartment = this.state.divisionFullList?.find(
        (a: UserDepartment) => a.divisionId === this.state.divisionSelectedKey,
      );

      var addplano = new UidInitiation();
      addplano.description = description?.trim().replace(/\s+/g, ' ') || '';
      addplano.planogram = planogramname?.trim().replace(/\s+/g, ' ') || '';
      addplano.familyName = familyname?.trim().replace(/\s+/g, ' ') || '';
      addplano.departmentId =
        departmentData?.departmentId == undefined
          ? this.state.divisionOptionlst[0].key
          : departmentData?.departmentId;
      addplano.divisionId =
        departmentData?.divisionId == undefined
          ? this.state.divisionOptionlst[0].key
          : departmentData?.departmentId;

      const planogramOrFamilyName = planogramname || familyname;

      const ifExist = await this.apiClient.getIfAlreadyExistorNot(planogramOrFamilyName);

      if (!ifExist.result) {
        const result = await this.apiClient.addPlanogramOrFamilyName(addplano);
        if (result.hasError) {
          this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
        } else {
          this.notify.showSuccessNotify(`${this.props.t('MSG-ADDED_SUCCESSFULLY')}`);
        }
        this.setState({ description: '', planogramName: '', familyName: '' });
        this.getDepartmentList();
      } else {
        this.setState({
          errors: {
            familyName: `${planogramOrFamilyName} is already exist`,
            planogramName: `${planogramOrFamilyName} is already exist`,
          },
        });
      }
    } catch (e: any) {
      this.notify.showErrorNotify(e.notify);
      this.setState({ familyname: '', planogramname: '' });
    }
  };

  async getDepartmentList() {
    var userId: any = this.authManager.getUserData()?.id;
    this.apiClient.getDepartmentDetails(userId).then((r) => {
      const departmentList: { key: number | undefined; text: string | undefined }[] = [];
      var i = 0;
      r?.result?.forEach((element: UserDepartment) => {
        if (!departmentList.find((e) => e.text == element.divisionName)) {
          departmentList.push({
            key: element.divisionId,
            text: element.divisionName,
          });
        }
      });
      let initialDivisionSelectedKey: number | null = null;
      if (departmentList.length > 0) {
        initialDivisionSelectedKey =
          departmentList[0].key !== undefined ? departmentList[0].key : null;
      }
      this.setState(
        {
          suggestions: [],
          planoOrFamilynameList: [],
          description: '',
          planogramname: '',
          familyname: '',
          divisionOptionlst: departmentList,
          divisionFullList: r?.result,
          divisionSelectedKey: initialDivisionSelectedKey,
        },
        () => {
          this.setState((prevState: any) => {
            return {
              divisionIsPlanoGram: prevState.divisionFullList?.find(
                (a: UserDepartment) => a.divisionId === prevState.divisionSelectedKey,
              )?.isPlanogram,
              showPlanogramFields: prevState.divisionSelectedKey === 61,
              showFamilyFields:
                prevState.divisionSelectedKey === 4 || prevState.divisionSelectedKey === 65,
            };
          });
        },
      );
    });
  }

  onChange = (option: IChoiceGroupOption) => {
    this.createPlanoLimitValue = '';
    this.dicriptionLimitValue = '';
    this.setState({
      errors: {
        planogramName: '',
        familyName: '',
        description: '',
      },
    });
    this.setState({
      divisionSelectedKey: option.key,
      divisionIsPlanoGram: this.state.divisionFullList?.find(
        (a: UserDepartment) => a.divisionId === parseInt(option.key),
      )?.isPlanogram,
      description: '',
      familyname: '',
      planogramname: '',
      suggestions: [],
      errors: { planogramName: '' },
    });
  };

  handleChange = async (e: any) => {
    const { value } = e.target;
    let trimmedValue = value.trim().replace(/[' "]/g).split(/ +/).join(' ');
    let ValueLength = trimmedValue.length;
    //let ValueLength = value.trim().split(/ +/).join(" ").length;
    const { planoOrFamilynameList, divisionIsPlanoGram, planOrFamilynameLength } = this.state;
    this.setState({ planogramname: value, familyname: value, errors: {} });
    if (ValueLength >= 3 && this.state.planoOrFamilynameList.length == 0) {
      if (ValueLength !== '   ') {
        this.apiClient
          .searchUID(value.trim().split(/ +/).join(' ').substring(0, 3), divisionIsPlanoGram)
          .then((r: any) => {
            this.state.planOrFamilynameLength < 3
              ? this.setState({ suggestions: [], planoOrFamilynameList: [] })
              : this.setState({
                  planoOrFamilynameList: r.result,
                  suggestions: r.result?.filter((user: any) => {
                    return (
                      user.familyName?.toLowerCase().includes(value.toLowerCase()) ||
                      user.planogram?.toLowerCase().includes(value.toLowerCase())
                    );
                  }),
                });
          });
      }
    } else if (value.trim().split(/ +/).join(' ').length > 2) {
      const newSuggestions = await planoOrFamilynameList.filter((user: any) => {
        return (
          user.familyName?.toLowerCase().includes(value.toLowerCase()) ||
          user.planogram?.toLowerCase().includes(value.toLowerCase())
        );
      });
      this.setState({
        suggestions: newSuggestions,
      });
    } else if (value.trim().split(/ +/).join(' ').length < 3) {
      this.setState({
        suggestions: [],
        planoOrFamilynameList: [],
      });
    }
  };
  createSuggestionsList = () => {
    const { suggestions } = this.state;
    return suggestions.map((user: any) => {
      return <div id="uidautocomplete">{user.familyName || user.planogram}</div>;
    });
  };

  validateForm = (fieldName: string) => {
    const { planogramname, familyname, description } = this.state;
    const errors: any = { ...this.state.errors };
    const setError = (field: string, message: string) => {
      errors[field] = message;
    };
    const isValidFormat = (value: string) => /^[a-zA-Z0-9\s]*[a-zA-Z][a-zA-Z0-9\s]*$/.test(value);
    switch (fieldName) {
      case 'planogramName':
        if (planogramname.trim() == '') {
          setError('planogramName', 'The Planogram Name field is required.');
        } else if (planogramname.trim().length > 50) {
          setError('planogramName', 'The Planogram Name can not be greater than 50 characters.');
        } else if (!isValidFormat(planogramname)) {
          setError('planogramName', 'The Planogram Name format is invalid.');
        } else {
          delete errors['planogramName'];
        }
        break;
      case 'familyName':
        if (familyname.trim() == '') {
          setError('familyName', 'The Family Name field is required.');
        } else if (familyname.trim().length > 50) {
          setError('familyName', 'The Family Name can not be greater than 50 characters.');
        } else if (!isValidFormat(familyname)) {
          setError('familyName', 'The Family Name format is invalid.');
        } else {
          delete errors['familyName'];
        }
        break;
      case 'description':
        if (description.trim() == '') {
          setError('description', 'The Description field is required.');
        } else if (description.trim().length >= 400) {
          setError('description', 'The Description can not be greater than 400 characters.');
        } else {
          delete errors['description'];
        }
        break;
      default:
        break;
    }
    this.setState({ errors });
    return Object.keys(errors).length === 0;
  };

  handleBlur = (fieldName: string) => {
    const { planogramname, familyname, description } = this.state;
    const errors: any = { ...this.state.errors };
    const setError = (field: string, message: string) => {
      errors[field] = message;
    };
    const isValidFormat = (value: string) => /^[a-zA-Z0-9\s]*[a-zA-Z][a-zA-Z0-9\s]*$/.test(value);
    switch (fieldName) {
      case 'planogramName':
        if (planogramname.trim() === '') {
          setError('planogramName', 'The Planogram Name field is required.');
        } else if (planogramname.trim().length > 50) {
          setError('planogramName', 'The Planogram Name can not be greater than 50 characters.');
        } else if (!isValidFormat(planogramname)) {
          setError('planogramName', 'The Planogram Name format is invalid.');
        } else {
          delete errors['planogramName'];
        }
        break;
      case 'familyName':
        if (familyname.trim() === '') {
          setError('familyName', 'The Family Name field is required.');
        } else if (familyname.trim().length > 50) {
          setError('familyName', 'The Family Name can not be greater than 50 characters.');
        } else if (!isValidFormat(familyname)) {
          setError('familyName', 'The Family Name format is invalid.');
        } else {
          delete errors['familyName'];
        }
        break;
      case 'description':
        if (description.trim() == '') {
          setError('description', 'The Description field is required.');
        } else if (description.trim().length > 400) {
          setError('description', 'The Description can not be greater than 400 characters.');
        } else {
          delete errors['description'];
        }
        break;
      default:
        break;
    }
    this.setState({ errors });
    return Object.keys(errors).length === 0;
  };

  validationMessage = (errorMessage: string) => {
    if (errorMessage.includes('planogramName may'))
      return errorMessage.replace('planogramName may', 'Planogram Name can');
    else if (errorMessage.includes('planogramName'))
      return errorMessage.replace('planogramName', 'Planogram Name');
    else if (errorMessage.includes('familyName may'))
      return errorMessage.replace('familyName may', 'Family Name can');
    else if (errorMessage.includes('familyName'))
      return errorMessage.replace('familyName', 'Family Name');
    else if (errorMessage.includes('may')) return errorMessage.replace('may', 'can');
    else return errorMessage;
  };

  render() {
    const { t } = this.props;
    return (
      <div>
        <form className="myForm" noValidate autoComplete="off" onSubmit={this.handleSubmit}>
          <Grid>
            {this.state.divisionSelectedKey ? (
              <>
                {this.state.divisionFullList?.find(
                  (a: UserDepartment) => a.divisionId === this.state.divisionSelectedKey,
                )?.isPlanogram ? (
                  <h1
                    style={{ fontSize: '28px', paddingLeft: '13px' }}
                    className="color-blue plano-family-title"
                  >
                    {t('PLANOGRAM_PAGE_TITLE')}
                  </h1>
                ) : (
                  <h1
                    style={{ fontSize: '28px', paddingLeft: '13px' }}
                    className="color-blue plano-family-title"
                  >
                    {t('FAMILYNAME_PAGE_TITLE')}
                  </h1>
                )}
              </>
            ) : (
              <h1
                style={{ fontSize: '28px', paddingLeft: '13px' }}
                className="color-blue plano-family-title"
              >
                {t('PLANOGRAM_PAGE_TITLE')}
              </h1>
            )}
          </Grid>

          <Grid style={{ marginTop: '5%', padding: 10 }}>
            <label className="planospan">{t('PLANOGRAM_DEPARTMENT')}</label>
            <Dropdown
              options={this.state.divisionOptionlst}
              onChange={(e: any, selectedOption: any) => {
                this.onChange(selectedOption);
                const divisionId = selectedOption.key;
                const division = this.state.divisionFullList.find(
                  (a: UserDepartment) => a.divisionId === divisionId,
                );
                this.setState({
                  showPlanogramFields: division?.isPlanogram === true,
                  showFamilyFields: division?.isPlanogram === false,
                });
              }}
              defaultSelectedKey={
                this.state.divisionOptionlst.length > 0
                  ? this.state.divisionOptionlst[0].key
                  : undefined
              }
              defaultValue={
                this.state.divisionOptionlst?.length === 1 ||
                this.state.divisionOptionlst?.length === 2
                  ? this.state.divisionOptionlst[0].key
                  : null
              }
              styles={departmentSelect}
              disabled={
                this.state.divisionOptionlst.length === 1 ||
                this.state.divisionOptionlst.length <= 1
              }
            />
          </Grid>

          {this.state.divisionSelectedKey ? (
            <>
              {this.state.divisionFullList?.find(
                (a: UserDepartment) => a.divisionId === this.state.divisionSelectedKey,
              )?.isPlanogram ? (
                <Grid style={{ marginTop: '5%', padding: 10 }}>
                  <Grid>
                    <label className="planospan">
                      {t('COL_PLANOGRAM_NAME')} <span className="required-marker">*</span>
                    </label>
                    <TextField
                      className="textField"
                      styles={textField}
                      name="planogramName"
                      // onBlur={this.form.handleBlurEvent}
                      onBlur={(event) => {
                        this.validateForm('planogramName');
                      }}
                      onChange={(event: any) => {
                        const enteredValue = event.target.value;
                        let capitalizedValue = enteredValue
                          .split(/\s+/)
                          .map((word: any) => {
                            const lowercaseWord = word.toLowerCase();
                            return lowercaseWord.charAt(0).toUpperCase() + lowercaseWord.slice(1);
                          })
                          .join(' ');
                        if (capitalizedValue.length >= 51) {
                          capitalizedValue = capitalizedValue.substring(0, 51);
                        }
                        event.target.value = capitalizedValue;
                        this.setState({ planOrFamilynameLength: capitalizedValue.length });
                        this.handleChange(event);
                        // this.form.handleChangeEvent(event);
                        this.setState(
                          {
                            planogramname: capitalizedValue,
                            familyname: '',
                            showPlanogramFields: true,
                            showFamilyFields: false,
                          },
                          () => {
                            this.validateForm('planogramName');
                          },
                        );
                      }}
                      value={this.state.planogramname}
                      placeholder={t('PLANOGRAM-PLACE-HOLDER')}
                      data-async
                      errorMessage={
                        this.state.errors.planogramName
                          ? this.validationMessage(this.state.errors.planogramName)
                          : ''
                      }
                    />
                    {this.state.suggestions && this.state.suggestions.length > 0 && (
                      <div className="suggestions-container">{this.createSuggestionsList()}</div>
                    )}
                  </Grid>
                </Grid>
              ) : (
                <Grid style={{ marginTop: '5%', padding: 10 }}>
                  <Grid>
                    <label className="planospan">
                      {t('COL_FAMILY_NAME')}
                      <span className="required-marker">*</span>
                    </label>
                    <TextField
                      className="textField"
                      styles={textField}
                      name="familyName"
                      // onBlur={this.form.handleBlurEvent}
                      onBlur={(event) => {
                        this.handleBlur('familyName');
                      }}
                      onChange={(event: any) => {
                        let enteredValue = event.target.value;
                        let capitalizedValue = enteredValue
                          .split(/\s+/)
                          .map((word: any) => {
                            const lowercaseWord = word.toLowerCase();
                            return lowercaseWord.charAt(0).toUpperCase() + lowercaseWord.slice(1);
                          })
                          .join(' ');
                        if (capitalizedValue.length >= 51) {
                          capitalizedValue = capitalizedValue.substring(0, 51);
                        }
                        event.target.value = capitalizedValue;
                        this.setState({ planOrFamilynameLength: capitalizedValue.length });
                        this.handleChange(event);
                        // this.form.handleChangeEvent(event);
                        this.setState(
                          {
                            familyname: capitalizedValue,
                            planogramname: '',
                            showPlanogramFields: false,
                            showFamilyFields: true,
                          },
                          () => {
                            this.validateForm('familyName');
                          },
                        );
                      }}
                      value={this.state.familyname}
                      placeholder={t('FAMILY_NAME-PLACE-HOLDER')}
                      data-async
                      errorMessage={
                        this.state.errors.familyName
                          ? this.validationMessage(this.state.errors.familyName)
                          : ''
                      }
                    />
                    {this.state.suggestions && this.state.suggestions.length > 0 && (
                      <div className="suggestions-container">{this.createSuggestionsList()}</div>
                    )}
                  </Grid>
                </Grid>
              )}
            </>
          ) : (
            <Grid style={{ marginTop: '5%', padding: 10 }}>
              <Grid>
                <label className="planospan">
                  {t('COL_PLANOGRAM_NAME')} <span className="required-marker">*</span>
                </label>
                <TextField
                  className="textField"
                  styles={textField}
                  name="planogramName"
                  // onBlur={this.form.handleBlurEvent}
                  onBlur={(event) => {
                    this.validateForm('planogramName');
                  }}
                  onChange={(event: any) => {
                    const enteredValue = event.target.value;
                    let capitalizedValue = enteredValue
                      .split(/\s+/)
                      .map((word: any) => {
                        const lowercaseWord = word.toLowerCase();
                        return lowercaseWord.charAt(0).toUpperCase() + lowercaseWord.slice(1);
                      })
                      .join(' ');
                    if (capitalizedValue.length >= 51) {
                      capitalizedValue = capitalizedValue.substring(0, 51);
                    }
                    event.target.value = capitalizedValue;
                    this.setState({ planOrFamilynameLength: capitalizedValue.length });
                    this.handleChange(event);
                    // this.form.handleChangeEvent(event);
                    this.setState({ planogramname: capitalizedValue, familyname: '' }, () => {
                      this.validateForm('planogramName');
                    });
                  }}
                  value={this.state.planogramname}
                  placeholder={t('PLANOGRAM-PLACE-HOLDER')}
                  data-async
                  errorMessage={
                    this.state.errors.planogramName
                      ? this.validationMessage(this.state.errors.planogramName)
                      : ''
                  }
                />
                {this.state.suggestions && this.state.suggestions.length > 0 && (
                  <div className="suggestions-container">{this.createSuggestionsList()}</div>
                )}
              </Grid>
            </Grid>
          )}

          <Grid style={{ marginTop: '5%', padding: 10 }}>
            <Grid>
              <label className="planospan">
                {t('PLANOGRAM_DISCRIPTION')}
                <span className="required-marker">*</span>
              </label>
              <TextField
                styles={textField}
                name="description"
                className="textField"
                // onBlur={this.form.handleBlurEvent}
                onBlur={(event) => {
                  this.handleBlur('description');
                }}
                onChange={(event: any) => {
                  let newValue = event.target.value.replace(/\s+/g, ' ');
                  if (newValue.length >= 400) {
                    newValue = newValue.substring(0, 400);
                  }
                  this.setState({ description: newValue }, () => {
                    this.validateForm('description');
                  });
                  // this.form.handleChangeEvent(event);
                }}
                placeholder={t('PLANOGRAM_DISCRIPTION_PLACE-HOLDER')}
                value={this.state.description}
                data-attribute-name="Description"
                data-async
                multiline
                resizable={false}
                errorMessage={
                  this.state.errors.description
                    ? this.validationMessage(this.state.errors.description)
                    : ''
                }
              />
            </Grid>
          </Grid>

          <Grid style={{ height: '50', padding: 10 }}>
            <Grid>
              <DefaultButton
                className="button"
                styles={uidCreate}
                type="submit"
                text={`${t('CREATE')}`}
                disabled={
                  Object.keys(this.state.errors).some(
                    (key) => this.state.errors[key]?.trim() !== '',
                  ) ||
                  (this.state.showFamilyFields
                    ? this.state.familyname.trim() == '' || this.state.description.trim() == ''
                    : this.state.planogramname.trim() == '' || this.state.description.trim() == '')
                }
              />
            </Grid>
          </Grid>
        </form>
      </div>
    );
  }
}

const ComponentTranslated: any = withTranslation()(FormPlanogramOrFamily);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
