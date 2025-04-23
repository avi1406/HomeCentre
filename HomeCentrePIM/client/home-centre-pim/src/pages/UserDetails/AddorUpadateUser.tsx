import ApiManager from '@/services/ApiManager';
import { DefaultButton, IDropdownOption, TextField } from '@fluentui/react';
import React from 'react';
import ReactFormValidation from 'react-form-input-validation';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import NotificationManager from '@/services/NotificationManager';
import {
  PIMcontentStyles,
  PIMfooterButtonCancel,
  PIMdropdownStyles,
  PIMfooterButtonConfirm,
  CheckboxStyles,
  PIMfooterButtonCancel1,
  PIMdropdownStyles1,
} from '../PimStyles';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { IChoiceGroupOption } from 'office-ui-fabric-react/lib/components/ChoiceGroup/ChoiceGroup.types';
import _ from 'lodash';
import { AppUserRole, AppUserStudioDeleteModal, AppUserStudioModal } from '@/services/ApiClient';

class AddorUpdateUser extends React.Component<any, any> {
  componentDidMount() {
    this.onAssign();
    this.setState({ initialSelectRoleLevel: this.state.selectrolelevel1 });
  }
  defaultOption = [{ name: 'Select Option', id: 0 }];
  apiClient = new ApiManager().CreateApiClient();
  form: ReactFormValidation;
  notify = new NotificationManager();
  RoleAddData: any = [];
  DivisionAddData: any = [];
  StudioAddData: any = [];
  constructor(props: any) {
    super(props);
    const allOptionsSelected =
      props.fields?.length === 1 &&
      props.fields[0].rolelevel?.includes('Level 1') &&
      props.fields[0].rolelevel?.includes('Level 2');

    this.state = {
      fields: {
        id: this.props.fields?.length === 1 ? this.props.fields[0]?.id : 0,
        isActive: this.props.fields?.length === 1 ? this.props.fields[0]?.isActive : true,
        firstName: this.props.fields?.length === 1 ? this.props.fields[0]?.firstName : '',
        lastName: this.props.fields?.length === 1 ? this.props.fields[0]?.lastName : '',
        mobile: this.props.fields?.length === 1 ? this.props.fields[0]?.mobile : '',
        email: this.props.fields?.length == 1 ? this.props.fields[0].email : '',
        rolelevel: this.props.fields?.length == 1 ? this.props.fields[0].rolelevel : null,
      },
      selectrolelevel11: [],
      initialSelectRoleLevel: [],
      Divselect: false,
      Roleselect: false,
      Studioselect: false,
      errors: {},
      selectDivision: [],
      roleData: [],
      DiviData: [],
      divisionData: [],
      selectdivision: '',
      divisionOptionlist: [],
      roleOptionlist: [],
      rolevalues: [],
      studioOptionlist: [],
      selectStudio: [],
      unSelectStudio: [],
      selectedStudioIds: [],
      StudioData: [],
      allStudioData: [],
      studiovalues: [],
      defaultSelectedStudio: [],
      divisionvalues: [],
      defaultSelectedDivision: '',
      defaultSelectedDivisionName: '',
      defaultSelectedRoles: [],
      selectedRoleIds: [],
      selectedDivisionsIDs: [],
      selectedDivisionID: [],
      selectrolelevel: '',
      checked1: false,
      // selectrolelevel1:
      //   this.props.fields?.length === 1
      //     ? this.props.fields[0].rolelevel === 'Level 1'
      //       ? [{ key: 'Level1', text: this.props.t('Level 1') }]
      //       : this.props.fields[0].rolelevel === 'Level 2'
      //       ? [{ key: 'Level2', text: this.props.t('Level 2') }]
      //       : null
      //     : null,
      allOptionsSelected,
      selectrolelevel1:
        props.fields?.length === 1
          ? [
              ...(allOptionsSelected ? [{ key: 'select-toggle', text: 'Select All' }] : []),
              ...(props.fields[0].rolelevel?.includes('Level 1')
                ? [{ key: 'Level1', text: props.t('Level 1') }]
                : []),
              ...(props.fields[0].rolelevel?.includes('Level 2')
                ? [{ key: 'Level2', text: props.t('Level 2') }]
                : []),
            ]
          : null,
      selectedval:
        this.props.fields?.length === 1
          ? this.props.fields[0].rolelevel === 'Level 1'
            ? { key: 'Level1', text: this.props.t('Level 1') }
            : this.props.fields[0].rolelevel === 'Level 2'
            ? { key: 'Level2', text: this.props.t('Level 2') }
            : null
          : null,
      unSelectRole: [],
      unSelectDivition: [],
      Ischeced: this.props.fields[0]?.isActive === true ? true : false,
      isActive1: this.props.fields[0]?.isActive === true ? true : false,
      checkInput: false,
      dropdownOptions: [
        { key: 'Level1', text: this.props.t('Level 1') },
        { key: 'Level2', text: this.props.t('Level 2') },
      ],
      dropdownOptions1: [],
      statusdropdown: '',
    };
    this.form = new ReactFormValidation(this);
    this.form.useRules({
      firstName: 'required|regex:/^[a-zA-Z ]*$/',
      lastName: 'required|regex:/^[a-zA-Z ]*$/',
      mobile: 'required|numeric|digits_between:10,12',
      email: 'required|regex:/^[a-zA-Z0-9+_.-]+@[a-zA-Z0-9.-]+$/|email',
      isActive: 'required|boolean',
    });
    this.form.onformsubmit = async (fields) => {
      try {
        this.props.ClosePopup();
        let response;
        let firstObject;
        let text;
        if (this.state.selectrolelevel1 !== null) {
          firstObject = this.state.selectrolelevel1[0] || '';
          text = firstObject.text;
        }
        if (this.state.checkInput) {
          const field = {
            ...fields,
            isActive: this.state.Ischeced,
            rolelevel: this.state.statusdropdown || text,
          };
          response = await this.apiClient.addOrUpdateUser(field);
        } else {
          const fields1 = {
            ...fields,
            rolelevel: this.state.statusdropdown || text,
          };
          response = await this.apiClient.addOrUpdateUser(fields1);
        }
        this.setState({ selectedRejectComment: null, otherComment: false });
        if (response.hasError) {
          this.notify.showErrorNotify(`${fields.email} ${this.props.t('MSG_EXISTS_ALREDY')}`);
        } else {
          await this.addOrRemoveUserDivision();
          await this.addOrRemoveUserRoles();
          await this.addOrRemoveUserStudio();
          this.notify.showSuccessNotify(
            fields.id === 0 ? `${this.props.t('MSG_ADD')}` : `${this.props.t('MSG_UPDATE')}`,
          );
        }
        this.props.Refresh();
      } catch (e: any) {
        this.notify.showErrorNotify(e.message);
      }
    };
  }

  handleDropdownChange = (event: any, option?: IDropdownOption, index?: number) => {
    if (option) {
      let selectedKeys = this.state.selectrolelevel1
        ? [...this.state.selectrolelevel1.map((item: any) => item.key)]
        : [];

      if (option.key === 'select-toggle') {
        if (option.selected) {
          selectedKeys = this.state.dropdownOptions.map((opt: any) => opt.key);
        } else {
          selectedKeys = [];
        }
      } else {
        if (option.selected) {
          if (!selectedKeys.includes(option.key as string)) {
            selectedKeys.push(option.key as string);
          }
        } else {
          selectedKeys = selectedKeys.filter((key) => key !== option.key);
        }
      }

      const allOptionsSelected = this.state.dropdownOptions.every((opt: any) =>
        selectedKeys.includes(opt.key),
      );

      if (allOptionsSelected && !selectedKeys.includes('select-toggle')) {
        selectedKeys.push('select-toggle');
      } else if (!allOptionsSelected && selectedKeys.includes('select-toggle')) {
        selectedKeys = selectedKeys.filter((key) => key !== 'select-toggle');
      }

      const updatedSelections = selectedKeys.map((key) => ({
        key,
        text: this.state.dropdownOptions.find((opt: any) => opt.key === key)?.text,
      }));

      const rolelevel = updatedSelections
        .filter((opt: any) => opt.text !== undefined)
        .map((opt: any) => opt.text)
        .join(',');

      this.setState({
        selectrolelevel1: updatedSelections,
        isDivision: true,
        rolelevel: ` ${rolelevel}`,
        statusdropdown: rolelevel,
      });
    }
  };

  // onAssign = async () => {
  //   const Roleresult = (await this.apiClient.getUserRoles(this.props.t(this.state.fields.id)))
  //     .result;
  //   const Roleresult1 = (await this.apiClient.getUserDepartment(this.props.t(this.state.fields.id)))
  //     .result;
  //   const Studioresult = (
  //     await this.apiClient.getUserStudioDetails(this.props.t(this.state.fields.id))
  //   ).result;
  //   const AllStudios = (await this.apiClient.getstudio()).result?.filter(
  //     (studio: any) => studio.isActive == true,
  //   );
  //   this.apiClient.getDivision().then((r: any) => {
  //     var divisiondatatype: any = [];
  //     r.result?.forEach((element: any) => {
  //       divisiondatatype.push({
  //         key: element.id,
  //         text: element.name,
  //       });
  //     });
  //     this.setState({
  //       divisionOptionlist: divisiondatatype,
  //     });
  //   });

  //   this.apiClient.getRoles(true).then((r: any) => {
  //     var roledatatype: any = [];
  //     r.result?.forEach((element: any) => {
  //       roledatatype.push({
  //         key: element.id,
  //         text: element.name,
  //       });
  //     });
  //     this.setState({ roleOptionlist: roledatatype });
  //   });

  //   this.apiClient.getstudio().then((r: any) => {
  //     const divisionIds = Roleresult1?.map((item: { id: any }) => item.id);
  //     const studiodatatype = r.result
  //       ?.filter(
  //         (studio: any) => divisionIds?.includes(studio.divisionId) && studio.isActive == true,
  //       )
  //       .map((element: any) => ({
  //         key: element.id,
  //         text: element.name,
  //       }));
  //     this.setState({ studioOptionlist: studiodatatype });
  //   });

  //   this.setState({ DiviData: Roleresult1, StudioData: Studioresult });
  //   this.setState({ allStudioData: AllStudios });
  //   this.DivisionAddData = this.state.DiviData.map((item: { id: any }) => item.id);
  //   this.StudioAddData = this.state.StudioData.map((item: { studioId: any }) => item.studioId);
  //   this.setState({ divisionvalues: this.DivisionAddData, studiovalues: this.StudioAddData });
  //   this.setState({
  //     selectDivision: this.state.divisionvalues,
  //     selectStudio: this.state.studiovalues,
  //   });
  //   this.setState({
  //     defaultSelectedDivision: this.state.divisionvalues,
  //     defaultSelectedStudio: this.state.studiovalues,
  //   });
  //   this.setState({ roleData: Roleresult });
  //   this.RoleAddData = this.state.roleData.map((item: { roleid: any }) => item.roleid);
  //   this.setState({ rolevalues: this.RoleAddData });
  //   this.setState({ selectrole: this.state.rolevalues });
  //   this.setState({ defaultSelectedRoles: this.state.rolevalues });
  // };
  onAssign = async () => {
    try {
      const userId = this.props.t(this.state.fields.id);
  
      const userRolesPromise = this.apiClient.getUserRoles(userId);
      const userDepartmentPromise = this.apiClient.getUserDepartment(userId);
      const userStudioDetailsPromise = this.apiClient.getUserStudioDetails(userId);
      const allStudiosPromise = this.apiClient.getstudio();
      const divisionPromise = this.apiClient.getDivision();
      const rolesPromise = this.apiClient.getRoles(true);
      const studioPromise = this.apiClient.getstudio();
  
      const [
        userRolesResponse,
        userDepartmentResponse,
        userStudioDetailsResponse,
        allStudiosResponse,
        divisionResponse,
        rolesResponse,
        studioResponse,
      ] = await Promise.all([
        userRolesPromise,
        userDepartmentPromise,
        userStudioDetailsPromise,
        allStudiosPromise,
        divisionPromise,
        rolesPromise,
        studioPromise,
      ]);
  
      const Roleresult = userRolesResponse?.result || [];
      const Roleresult1 = userDepartmentResponse?.result || [];
      const Studioresult = userStudioDetailsResponse?.result || [];
      const AllStudios = allStudiosResponse?.result?.filter((studio: any) => studio.isActive) || [];
  
      const divisiondatatype = divisionResponse?.result?.map((element: any) => ({
        key: element.id,
        text: element.name,
      })) || [];
  
      this.setState({
        divisionOptionlist: divisiondatatype,
      });
  
      const roledatatype = rolesResponse?.result?.map((element: any) => ({
        key: element.id,
        text: element.name,
      })) || [];
  
      this.setState({ roleOptionlist: roledatatype });
  
      const divisionIds = Roleresult1.map((item: { id: any }) => item.id);
      const studiodatatype = studioResponse?.result
        ?.filter(
          (studio: any) => divisionIds.includes(studio.divisionId) && studio.isActive,
        )
        .map((element: any) => ({
          key: element.id,
          text: element.name,
        })) || [];
  
      this.setState({ studioOptionlist: studiodatatype });
      this.setState({
        DiviData: Roleresult1,
        StudioData: Studioresult,
        allStudioData: AllStudios,
        divisionvalues: Roleresult1.map((item: { id: any }) => item.id),
        studiovalues: Studioresult.map((item: { studioId?: any }) => item.studioId),
        selectDivision: Roleresult1.map((item: { id: any }) => item.id),
        selectStudio: Studioresult.map((item: { studioId?: any }) => item.studioId),
        defaultSelectedDivision: Roleresult1.map((item: { id: any }) => item.id),
        defaultSelectedStudio: Studioresult.map((item: { studioId?: any }) => item.studioId),
        roleData: Roleresult,
        rolevalues: Roleresult.map((item: { roleid: any }) => item.roleid),
        selectrole: Roleresult.map((item: { roleid: any }) => item.roleid),
        defaultSelectedRoles: Roleresult.map((item: { roleid: any }) => item.roleid),
      });
  
    } catch (error: any) {
      console.error("Error in onAssign:", error);
      this.notify.showErrorNotify(error.message);
    }
  };  

  DivisionRoleAddOrRemove = () => {
    this.addOrRemoveUserDivision();
  };
  addOrRemoveUserDivision = async () => {
    var Body = new AppUserRole();
    const { email } = this.state.fields;
    var isActive = true;
    const apiResponse: any = (await this.apiClient.searchUsers(isActive)).result?.records;
    const filteredRecord = apiResponse.find((record: any) => {
      return record.email === email;
    });
    const fieldsId = this.props.fields[0]?.id;
    Body.appUserId = fieldsId || (filteredRecord && filteredRecord.id);
    const appUserId1 = fieldsId || (filteredRecord && filteredRecord.id);
    Body.name = '';
    Body.description = '';
    const data: any = {
      appUserid: Body.appUserId,
      appUserRoleDomainModel: this.state.selectedDivisionsIDs,
    };
    if (appUserId1) {
      const response = (await this.apiClient.addDepartmentToUser(data)).result;
    }
    this.state.unSelectDivition.forEach((value: number | undefined) => {
      if (Body.appUserId) {
        Body.departmentId = value;
        this.apiClient.deleteDepartmentToUser(Body);
      }
    });
  };

  addOrRemoveUserRoles = async () => {
    const { email } = this.state.fields;
    var isActive = true;
    const apiResponse: any = (await this.apiClient.searchUsers(isActive)).result?.records;
    const filteredRecord = apiResponse.find((record: any) => {
      return record.email === email;
    });
    const appUserId = this.props.fields[0]?.id || (filteredRecord && filteredRecord.id);
    const data: any = {
      appUserid: appUserId,
      appUserRoleDomainModel: this.state.selectedRoleIds,
    };
    if (appUserId) {
      const response1 = (await this.apiClient.addUserRole(data)).result;
    }
    this.state.unSelectRole.forEach((value: number | undefined) => {
      if (appUserId) {
        this.apiClient.deleteRoles(appUserId, value);
      }
    });
  };

  addOrRemoveUserStudio = async () => {
    var addBody = new AppUserStudioModal();
    var deleteBody = new AppUserStudioDeleteModal();
    const { email } = this.state.fields;
    var isActive = true;
    const apiResponse: any = (await this.apiClient.searchUsers(isActive)).result?.records;
    const filteredRecord = apiResponse.find((record: any) => {
      return record.email === email;
    });
    const fieldsId = this.props.fields[0]?.id;
    addBody.userId = fieldsId || (filteredRecord && filteredRecord.id);
    deleteBody.userId = fieldsId || (filteredRecord && filteredRecord.id);
    const userId = fieldsId || (filteredRecord && filteredRecord.id);
    addBody.studioId = this.state.selectedStudioIds;

    if (userId) {
      const response = (await this.apiClient.addStudioToUser(addBody)).result;
    }
    this.state.unSelectStudio.forEach(async (value: number | undefined) => {
      if (userId) {
        deleteBody.studioId = value;
        await this.apiClient.deleteStudioFromUser(deleteBody);
      }
    });
  };

  handleCheckboxChange = () => {
    this.setState({
      Ischeced: this.state.Ischeced == false ? true : false,
      checkInput: this.state.Ischeced === this.props.fields[0]?.isActive,
    });
  };
  onRoleChange = (event: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) => {
    const selectedValues = [...this.state.selectrole];
    let selectedRole: any[] = [];
    if (selectedValues.includes(option.key)) {
      const index = selectedValues.indexOf(option.key);
      if (index !== -1) {
        selectedValues.splice(index, 1);
        if (
          this.state.roleData.some((role: any) => role.roleid === option.key) &&
          !selectedValues.includes(option.key)
        ) {
          this.setState(
            (prevState: any) => ({
              unSelectRole: [...prevState.unSelectRole, option.key],
            }),
            () => {},
          );
        }
        this.setState((prevState: any) => ({
          selectedRoleIds: prevState.selectedRoleIds.filter(
            (role: any) => role.roleId !== option.key,
          ),
        }));
      }
    } else {
      selectedValues.push(option.key);
      this.setState(
        (prevState: any) => ({
          unSelectRole: prevState.unSelectRole.filter((id: any) => id !== option.key),
        }),
        () => {},
      );
    }
    selectedRole = selectedValues.map((roleId) => {
      const roleOption = this.state.roleOptionlist.find((opt: any) => opt.key === roleId);
      return {
        roleId: roleId,
        departmentId: 0,
        name: roleOption ? roleOption.text : '',
      };
    });

    this.setState(
      {
        selectrole: selectedValues,
        Roleselect: selectedValues.length === 0,
        selectedRoleIds: selectedRole,
      },
      () => {
        if (this.state.selectStudio && !this.state.selectrole.includes(16)) {
          this.setState(
            (prevState: any) => ({
              unSelectStudio: [...prevState.unSelectStudio, ...this.state.selectStudio],
            }),
            () => {
              this.setState({ selectedStudioIds: [], selectStudio: [], defaultSelectedStudio: [] });
            },
          );
        }
      },
    );
  };

  onStudioChange = (option: IChoiceGroupOption) => {
    const selectedValues = [...this.state.selectStudio];
    let selectedStudio: any[] = [];
    if (selectedValues.includes(option.key)) {
      const index = selectedValues.indexOf(option.key);
      if (index !== -1) {
        selectedValues.splice(index, 1);
        if (
          this.state.StudioData.some((studio: any) => studio.studioId === option.key) &&
          !selectedValues.includes(option.key)
        ) {
          this.setState(
            (prevState: any) => ({
              unSelectStudio: [...prevState.unSelectStudio, option.key],
            }),
            () => {},
          );
        }
        this.setState((prevState: any) => ({
          selectedStudioIds: prevState.selectedStudioIds.filter(
            (studio: any) => studio.studioId !== option.key,
            () => {},
          ),
        }));
      }
    } else {
      selectedValues.push(option.key);
      this.setState(
        (prevState: any) => ({
          unSelectStudio: prevState.unSelectStudio.filter((id: any) => id !== option.key),
        }),
        () => {},
      );
    }
    selectedStudio = selectedValues.map((studioId) => {
      const studioOption = this.state.studioOptionlist.find((opt: any) => opt.key === studioId);
      return studioId;
    });

    this.setState(
      {
        selectStudio: selectedValues,
        Studioselect: selectedValues.length === 0,
        selectedStudioIds: selectedStudio,
      },
      () => {},
    );
  };

  onRoleChange1 = (event: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) => {
    const selectedValues = [...this.state.selectDivision];
    let selectedDivisions: any[] = [];
    if (selectedValues.includes(option.key)) {
      const index = selectedValues.indexOf(option.key);
      if (index !== -1) {
        selectedValues.splice(index, 1);
        if (
          this.state.DiviData.some((division: any) => division.id === option.key) &&
          !selectedValues.includes(option.key)
        ) {
          this.setState((prevState: any) => ({
            unSelectDivition: [...prevState.unSelectDivition, option.key],
          }));
        }
        this.setState((prevState: any) => ({
          selectedDivisionsIDs: prevState.selectedDivisionsIDs.filter(
            (division: any) => division.departmentId !== option.key,
          ),
        }));
      }
    } else {
      selectedValues.push(option.key);
      this.setState((prevState: any) => ({
        unSelectDivition: prevState.unSelectDivition.filter((id: any) => id !== option.key),
      }));
    }

    selectedDivisions = selectedValues
      .filter((divisionKey) => !this.state.unSelectDivition.includes(divisionKey))
      .map((divisionKey) => ({
        roleId: 0,
        departmentId: divisionKey,
        name: option.text,
      }));
    this.setState(
      {
        selectDivision: selectedValues,
        Divselect: selectedValues.length === 0,
        selectedDivisionsIDs: selectedDivisions,
      },
      () => {
        const selecteddata = this.state.selectStudio;
        if (this.state.selectDivision && this.state.selectDivision.length !== 0) {
          this.setState({ studioOptionlist: [] });
          var studiodatatype = this.state.allStudioData
            ?.filter((studio: any) => this.state.selectDivision.includes(studio.divisionId))
            .map((element: any) => ({
              key: element.id,
              text: element.name,
            }));

          this.setState({ studioOptionlist: studiodatatype }, () => {
            const resetSelectStudio = this.state.selectStudio.some(
              (studioId: any) => !studiodatatype.some((studio: any) => studio.key === studioId),
            );

            if (resetSelectStudio) {
              this.setState(
                (prevState: any) => ({
                  unSelectStudio: [...prevState.unSelectStudio, ...this.state.selectStudio],
                }),
                () => {
                  this.setState({
                    selectedStudioIds: [],
                    selectStudio: [],
                    defaultSelectedStudio: [],
                  });
                },
              );
            }
          });
        } else {
          this.setState(
            (prevState: any) => ({
              unSelectStudio: [...prevState.unSelectStudio, ...this.state.selectStudio],
            }),
            () => {
              this.setState({ selectedStudioIds: [], selectStudio: [], defaultSelectedStudio: [] });
            },
          );
        }
      },
    );
  };

  render() {
    const { fields1, t } = this.props;
    const selectedKeys1 = fields1 ? fields1.map((item: any) => item.key) : [];
    return (
      <>
        <form className="myForm" noValidate autoComplete="off" onSubmit={this.form.handleSubmit}>
          {(this.props.fields?.length === 1 ? true : false) && (
            <div className={PIMcontentStyles.attributeBody1}>
              <Grid item lg={10} xs={12}>
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
              </Grid>
            </div>
          )}
          <div className={PIMcontentStyles.attributeBody}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  label={`${this.props.t('USER_FIRST_NAME')}`}
                  name="firstName"
                  className="textField"
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields?.firstName}
                  data-attribute-name="First Name"
                  data-async
                  required
                  errorMessage={
                    this.state.errors.firstName
                      ? this.state.errors.firstName === 'The First Name format is invalid.'
                        ? 'The First Name Allows only Alphabets'
                        : this.state.errors.firstName
                      : ''
                  }
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  label={`${this.props.t('USER_LAST_NAME')}`}
                  name="lastName"
                  className="textField"
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields?.lastName}
                  data-attribute-name="Last Name"
                  data-async
                  required
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                  errorMessage={
                    this.state.errors.lastName
                      ? this.state.errors.lastName === 'The Last Name format is invalid.'
                        ? 'The Last Name Allows only Alphabets'
                        : this.state.errors.lastName
                      : ''
                  }
                />
              </Grid>
              <Grid item lg={0.75} xs={12} />
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  label={`${this.props.t('USER_TABLE_MOBILE')}`}
                  name="mobile"
                  type="tel"
                  className="textField"
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  value={this.state.fields?.mobile}
                  data-attribute-name="Mobile"
                  data-async
                  required
                  onKeyDown={(evt) =>
                    ['e', 'E', '+', '-'].includes(evt.key) && evt.preventDefault()
                  }
                  errorMessage={this.state.errors.mobile ? this.state.errors.mobile : ''}
                  disabled={
                    !(this.state.isActive1 === true
                      ? true
                      : false || (this.props.fields?.length === 1 ? false : true))
                  }
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
              <Grid item lg={4.5} xs={12}>
                <TextField
                  name="email"
                  required
                  className="textField"
                  label={`${this.props.t('USER_TABLE_EMAIL')}`}
                  value={this.state.fields?.email}
                  onBlur={this.form.handleBlurEvent}
                  onChange={(event: any) => this.form.handleChangeEvent(event)}
                  disabled={this.props.fields?.length === 1 ? true : false}
                  data-attribute-name="Email"
                  data-async
                  errorMessage={this.state.errors.email ? this.state.errors.email : ''}
                />
              </Grid>
              <Grid item lg={0.95} xs={12} />
            </Grid>
            {this.props.t(this.state.fields?.id) !== 0 ? (
              <>
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  <Grid item lg={0.95} xs={12} />
                  <Grid item lg={4.5} xs={12}>
                    <Dropdown
                      id="Division"
                      className="dropDown"
                      required
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      label={`${this.props.t('USER_BTN_ASSIGN_DIVISION')}`}
                      options={this.state.divisionOptionlist}
                      onChange={(e: any, selectedOptions: any) =>
                        this.onRoleChange1(e, selectedOptions)
                      }
                      styles={PIMdropdownStyles}
                      selectedKeys={this.state.selectDivision}
                      multiSelect={!this.state.isAssignDivision}
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
                      id="Roles"
                      className="dropDown"
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      label={`${this.props.t('USER_BTN_ASSIGN_ROLES')}`}
                      options={this.state.roleOptionlist}
                      required
                      selectedKeys={this.state.selectrole}
                      onChange={(e: any, selectedOptions: any) =>
                        this.onRoleChange(e, selectedOptions)
                      }
                      styles={PIMdropdownStyles}
                      multiSelect={!this.state.isAssignDepartment}
                      disabled={
                        !(this.state.isActive1 === true
                          ? true
                          : false || (this.props.fields?.length === 1 ? false : true))
                      }
                    />
                  </Grid>
                </Grid>
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  <Grid item lg={0.95} xs={12} />
                  <Grid item lg={4.5} xs={12}>
                    <Dropdown
                      id="Studio"
                      className="dropDown"
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      label={`${this.props.t('USER_BTN_STUDIO')}`}
                      options={this.state.studioOptionlist}
                      onChange={(e: any, selectedOptions: any) =>
                        this.onStudioChange(selectedOptions)
                      }
                      styles={PIMdropdownStyles}
                      selectedKeys={this.state.selectStudio}
                      multiSelect={!this.state.isAssignDepartment}
                      disabled={
                        !(this.state.isActive1 === true
                          ? true
                          : false || (this.props.fields?.length === 1 ? false : true)) ||
                        !(this.state.selectrole && this.state.selectrole.includes(16)) ||
                        !(this.state.selectDivision && this.state.selectDivision.length !== 0)
                      }
                    />
                  </Grid>
                  <Grid item lg={0.95} xs={12}></Grid>
                  <Grid item lg={4.5} xs={12}>
                    <Dropdown
                      id="RolesLevel"
                      className="dropDown"
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      label={`${this.props.t('ROLE_LEVEL')}`}
                      options={[
                        {
                          key: 'select-toggle',
                          text: 'Select All',
                          selected:
                            (this.state.selectrolelevel1 || []).length ===
                            (this.state.dropdownOptions || []).length,
                        },
                        ...(this.state.dropdownOptions || []).map((option: any) => ({
                          key: option.key,
                          text: option.text,
                          selected: (this.state.selectrolelevel1 || []).includes(option.key),
                        })),
                      ]}
                      selectedKeys={
                        this.state.selectrolelevel1
                          ? this.state.selectrolelevel1.map((item: any) => item.key)
                          : []
                      }
                      styles={PIMdropdownStyles1}
                      disabled={
                        !(this.state.isActive1 === true
                          ? true
                          : false || (this.props.fields?.length === 1 ? false : true))
                      }
                      multiSelect
                      onChange={this.handleDropdownChange}
                    />
                  </Grid>
                </Grid>
              </>
            ) : (
              ' '
            )}
            <br />
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  styles={PIMfooterButtonConfirm}
                  type="submit"
                  className="button"
                  disabled={
                    this.props.fields.length === 1
                      ? !(this.state.Divselect == true) &&
                        !(this.state.Roleselect == true) &&
                        (!(this.state.fields?.firstName === this.props.fields[0]?.firstName) ||
                          !(this.state.fields?.lastName === this.props.fields[0]?.lastName) ||
                          !(this.state.fields?.mobile === this.props.fields[0]?.mobile) ||
                          !(this.state.Ischeced === this.props.fields[0]?.isActive) ||
                          !(this.state.checked1 === false) ||
                          !_.isEqual(
                            _.sortBy(this.state.defaultSelectedRoles),
                            _.sortBy(this.state.selectrole),
                          ) ||
                          !_.isEqual(
                            _.sortBy(this.state.defaultSelectedDivision),
                            _.sortBy(this.state.selectDivision),
                          ) ||
                          (this.state.fields?.id !== 0 &&
                            !_.isEqual(
                              _.sortBy(this.state.defaultSelectedStudio),
                              _.sortBy(this.state.selectStudio),
                            )) ||
                          !_.isEqual(
                            _.sortBy(this.state.selectrolelevel1.map((item: any) => item.key)),
                            _.sortBy(
                              this.state.initialSelectRoleLevel.map((item: any) => item.key),
                            ),
                          ))
                        ? false
                        : true
                      : !this.state.fields?.firstName ||
                        !this.state.fields?.lastName ||
                        !this.state.fields?.email ||
                        !this.state.fields?.mobile ||
                        !this.state.selectDivision ||
                        this.state.selectDivision.length === 0 ||
                        !this.state.selectrole ||
                        this.state.selectrole.length === 0 ||
                        (this.state.fields?.id !== 0 && this.state.selectStudio.length === 0)
                  }
                  onClick={this.DivisionRoleAddOrRemove.bind(this)}
                >
                  {this.props.t(this.state.fields?.id !== 0 ? 'EDIT_BTN_SUBMIT' : 'ADD_BTN_SUBMIT')}
                </DefaultButton>
              </Grid>

              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  text={`${t('BTN_CANCEL')}`}
                  className="button"
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
export default AddorUpdateUser;
