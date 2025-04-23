import React, { Component, Suspense } from 'react';
import { Grid } from '@mui/material';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import ApiManager from '@/services/ApiManager';
import {
  Async,
  ComboBox,
  DatePicker,
  DefaultButton,
  Dropdown,
  IChoiceGroupOption,
  IContextualMenuProps,
  IDropdownOption,
  Icon,
  Modal,
  PrimaryButton,
  SearchBox,
  Stack,
  TextField,
  mergeStyles,
} from '@fluentui/react';
import DataTable from 'react-data-table-component';
import {
  PIMButtons,
  columnHeader,
  PIMHeaderButtons,
  productDropdown,
  ImgIcons1,
  ImgIcons,
  PIMsearchBoxStyles,
  Alartmessage,
  PIMdropdownStyles2,
  PIMHearderText,
  PIMcontentStyles,
  PIMsearchBoxStyles123,
  PIMButtons123,
} from '@/pages/PimStyles';
import { Link } from 'react-router-dom';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import {
  downloadFileItemcode,
  downloadFileItemcodeAll,
  downloadFileItemcodeReady,
  downloadFileItemcodeSearch,
  downloadFileMarketPlace,
  renderOption,
} from '@/pages/commonFunctions';
import { baseUrl } from '@/services/Constants';
import CommonDate, { CommonDateRef } from '../../commonFunctions';
import axios from 'axios';
import Util from '@/services/Util';
import { MarketplaceTemplate } from '@/services/ApiClient';
import { t } from 'i18next';

class MarketTemplate extends Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  selectedMarketPlace: any;
  roleId: any = this.decode.RoleId.split(',').map(Number);
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  util = new Util();
  statusOptions: any = [
    { key: 0, text: this.props.t('ALL') },
    { key: 2, text: this.props.t('NOT_READY') },
    { key: 1, text: this.props.t('READY') },
  ];
  selectedDivision: any = 0;
  constructor(props: any) {
    super(props);
    this.state = {
      templateDropdownOption: [],
      templateDatas: [],
      searchData: [],
      searchData1: [],
      divisionOption: [],
      searchData2: [],
      isRecord: false,
      isDropdownOpen: false,
      isActive: true,
      marketPlaceTemplateId: 0,
      userDivision: null,
      productDeptId: null,
      departmentOption: [],
      departmentOption1: [],
      marketPlaceId: 1,
      marketplaceName: this.props.t('SELECT_MARKETPLACE'),
      fromDate: null,
      toDate: null,
      searchKey: '',
      Isdata: false,
      statusOptions: 0,
      previousStatusOptions: 0,
      mapingbool: false,
      deptOption: [],
      searchDivision1: null,
      Download: false,
      readyAll: false,
      MRenable: false,
      Searchenable: false,
      downloadenable: false,
      searchValue: '',
      searchKey1: '',
      searchValue1: '',
      steusRead: -1,
      productDeptId1: [],
      refreshKey: 0,
      DivdeptOption: [],
      refreshKey1: 0,
      planoSearchText: '',
    };
  }
  commonDateRef = React.createRef<CommonDateRef>();

  // async componentDidMount(): Promise<void> {
  //   this.setState({ isRecord: true });
  //    this.FilterFun();
  //   var division: any = [];
  //   var divisionId: any = this.authManager.getUserData()?.departmentId;
  //   const divisions = await this.apiClient.getDivision();
  //   const MasterData = (await this.apiClient.getHierarchicalMasters()).result;
  //   const departmentSet = new Set();
  //   MasterData?.forEach((element: any) => {
  //     if (element && this.deptId.includes(element.division)) {
  //       const dept = {
  //         dept: element.dept,
  //         Name: element.dept_Name,
  //         division: element.division,
  //       };
  //       const isDuplicate = Array.from(departmentSet).some((item: any) => item.dept === dept.dept);
  //       if (!isDuplicate) {
  //         departmentSet.add(dept);
  //       }
  //     }
  //   });

  //   const department = Array.from(departmentSet).map((deptName: any) => ({
  //     key: deptName.dept,
  //     text: deptName.Name,
  //     division: deptName.division,
  //   }));
  //   divisions?.result?.forEach((element: any) => {
  //     division.push({ key: element.id, text: element.name });
  //   });
  //   var filteredDivision = division?.filter((o1: any) =>
  //     divisionId.some((o2: any) => o1.key === o2),
  //   );
  //   var option: any = [];
  //   var deptOpt: any = [];
  //   const tempdrop = await this.apiClient.searchMarketplaces('', 0, 0, true);
  //   const depyResponse = (await this.apiClient.getDepartments()).result;
  //   tempdrop.result?.records?.forEach((element: any) => {
  //     option.push({ key: element.id, text: element.name });
  //   });
  //   var deptOptCG: {
  //     key: number | undefined;
  //     text: string | undefined;
  //     parentCode: number | undefined;
  //   }[] = [];
  //   var CatogaryDropdown: {
  //     key: number | undefined;
  //     text: string | undefined;
  //     parentCode: number | undefined;
  //   }[] = [];
  //   department?.forEach((e: any) => {
  //     const parentCodeAsNumber = parseInt(e.division, 10);
  //     if (this.deptId.includes(parentCodeAsNumber)) {
  //       CatogaryDropdown.push({ key: e.key, text: e.text, parentCode: e.division });
  //     }
  //     deptOptCG.push({ key: e.dept, text: e.name, parentCode: e.division });
  //   });

  //   const filteredDivOpt1 = deptOptCG?.filter((department: any) => {
  //     return filteredDivision.some(
  //       (filter: any) => department.parentCode === filter.key.toString(),
  //     );
  //   });
  //   filteredDivOpt1?.forEach((element) => {
  //     deptOpt.push({ key: element.key, text: element.text });
  //   });
  //   this.setState({
  //     // departmentOption: deptOpt,
  //     // departmentOption1: CatogaryDropdown,
  //     productDeptId1: [],
  //     DivdeptOption: [],
  //   });
  //   // this.selectedMarketPlace = option?.filter((element: any) => element.key == 1)[0];
  //   var departmentList = (await this.apiClient.getDivision())?.result;

  //   var deptOpt112: { key: number | undefined; text: string | undefined }[] = [];
  //   departmentList?.forEach((element) => {
  //     deptOpt112.push({ key: element.id, text: element.name });
  //   });
  //   var filteredDivOpt = deptOpt112?.filter((department) =>
  //     filteredDivision.some((filter: any) => department.key === filter.key),
  //   );
  //   var deptOpt1: { key: number | undefined; text: string | undefined }[] = [];
  //   filteredDivOpt?.forEach((element) => {
  //     deptOpt1.push({ key: element.key, text: element.text });
  //   });
  //   this.setState({
  //     deptOption: deptOpt1,
  //   });
  //   this.refresh(this.state.isActive, null);
  // }
  async componentDidMount(): Promise<void> {
    this.setState({ isRecord: true });
  
    try {
      this.FilterFun();
  
      const [divisionsResponse, masterDataResponse, tempdropResponse, depyResponse] = await Promise.all([
        this.apiClient.getDivision(),
        this.apiClient.getHierarchicalMasters(),
        this.apiClient.searchMarketplaces('', 0, 0, true),
        this.apiClient.getDepartments()
      ]);
  
      const MasterData = masterDataResponse?.result || [];
      const divisions = divisionsResponse?.result || [];
      const tempdrop = tempdropResponse?.result?.records || [];
  
      const departmentSet = new Set();
      MasterData.forEach((element: any) => {
        if (element && this.deptId.includes(element.division)) {
          const dept = {
            dept: element.dept,
            Name: element.dept_Name,
            division: element.division,
          };
          if (!Array.from(departmentSet).some((item: any) => item.dept === dept.dept)) {
            departmentSet.add(dept);
          }
        }
      });
  
      const department = Array.from(departmentSet).map((deptName: any) => ({
        key: deptName.dept,
        text: deptName.Name,
        division: deptName.division,
      }));
  
      const divisionId = this.authManager.getUserData()?.departmentId;
      const filteredDivision = divisions.filter((division: any) => 
        divisionId?.some((id: any) => division.id === id)
      );
  
      const option = tempdrop.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      const deptOptCG = department.map((e: any) => ({
        key: e.key,
        text: e.text,
        parentCode: parseInt(e.division, 10),
      }));
  
      const CatogaryDropdown = deptOptCG.filter((e: any) =>
        this.deptId.includes(e.parentCode)
      );
  
      const filteredDivOpt1 = deptOptCG.filter((department: any) =>
        filteredDivision.some((filter: any) => department.parentCode === filter.id)
      );
  
      const deptOpt = filteredDivOpt1.map((element: any) => ({
        key: element.key,
        text: element.text,
      }));
  
      this.setState({
        productDeptId1: [],
        DivdeptOption: [],
      });
  
      const departmentList = (await this.apiClient.getDivision())?.result || [];
      const deptOpt112 = departmentList.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      const filteredDivOpt = deptOpt112.filter((department: any) =>
        filteredDivision.some((filter: any) => department.key === filter.id)
      );
  
      const deptOpt1 = filteredDivOpt.map((element: any) => ({
        key: element.key,
        text: element.text,
      }));
  
      this.setState({
        deptOption: deptOpt1,
      });
  
      this.refresh(this.state.isActive, null);
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
  }  
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  async refresh(isActive: boolean, productId?: any) {
    this.setState({ isRecord: true });
    var option: any = [];
    const tempdrop = await this.apiClient.searchMarketplaces('', 0, 0, true);
    tempdrop.result?.records?.forEach((element: any) => {
      option.push({ key: element.id, text: element.name });
    });
    const options = option.filter((item: any) => item.key !== 1);
    this.selectedMarketPlace = option?.filter((element: any) => element.key == 1)[0];
    this.setState({
      templateDatas: [],
      searchKey: '',
      searchValue: '',
      templateDropdownOption: options,
    });
    const Ids: any = this.util.getUrlParameterId();
    var divOpt: any = [];
    const id: any = Ids.split(',');
    const viewDataId: any = parseInt(id[0]);
    const div = (await this.apiClient.getDivision()).result;
    div?.forEach((e: any) => {
      divOpt.push({ key: e.id, text: e.name });
    });
    var marketPlaceId =
      this.selectedMarketPlace?.key == undefined ? 1 : this.selectedMarketPlace.key;
    let response;
    const productDepartmentId: number[] = [];
    this.state.departmentOption1
      .filter((row: any) => this.state.productDeptId1.includes(row.dept_Name.trim()))
      .forEach((row: any) => {
        const keyAsNumber = Number(row.id);
        if (!isNaN(keyAsNumber) && !productDepartmentId.includes(keyAsNumber)) {
          productDepartmentId.push(keyAsNumber);
        }
      });
    let resultString;
    if (this.state.DivdeptOption.length == 0) {
      resultString = this.deptId.filter((item) => item !== 0);
    } else {
      resultString = this.state.DivdeptOption.map((divDeptText: string) => {
        const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
        return division ? division.key : null;
      }).filter((key: any) => key !== null);
    }
    response = await this.apiClient.searchMarketplaceTemplates(
      '',
      0,
      0,
      marketPlaceId,
      resultString,
      productDepartmentId,
      isActive,
    );
    const records = response.result?.records || [];
    const nameToSet = records.length > 0 ? records[0].name : '';
    const firstRecord = records.length > 0 ? records[0] : null;

    this.setState({
      divisionOption: divOpt,
      marketPlaceId: this.state.marketPlaceId,
      productDeptId: productDepartmentId,
      refreshKey: this.state.refreshKey + 1,
      refreshKey1: this.state.refreshKey1 + 1,
      id: firstRecord?.id,
      marketPlaceTemplateId: viewDataId,
    });
    this.setState({
      searchAdmindata: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
    var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
    var planoSearchText =
      this.state.planoSearchText == undefined ? null : this.state.planoSearchText;
    if (marketplaceTemplateId == 0 || marketplaceTemplateId != undefined) {
      var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
      var status: any = this.state.statusOptions;
      var fromDate = this.state.fromDate
        ? new Date(this.state.fromDate.getTime() + 24 * 60 * 60 * 1000).toISOString()
        : '';
      var toDate = this.state.toDate
        ? new Date(this.state.toDate.getTime() + 24 * 60 * 60 * 1000).toISOString()
        : '';
      const divisionSet = new Set();
      const division =
        this.state.searchAdmindata &&
        this.state.searchAdmindata
          .filter(
            (e: any) =>
              e.divisionId && !divisionSet.has(e.divisionId) && divisionSet.add(e.divisionId),
          )
          .map((e: any) => e.divisionId)
          .join(',');
      let DepartmentIds;
      if (this.state.productDeptId1.length != 0 && this.state.productDeptId1 != null) {
        let departmentSet = new Set();
         DepartmentIds = this.state.searchAdmindata
          .filter((e: any) => e.productDepartmentId)
          .map((e: any) => {
            let matchingDept = this.state.departmentOption1.find(
              (dept: any) => dept.id === e.productDepartmentId,
            );
            if (matchingDept && !departmentSet.has(matchingDept.dept)) {
              departmentSet.add(matchingDept.dept);
              return matchingDept.dept;
            } else {
              return null;
            }
          })
          .filter((dept: any) => dept !== null)
          .join(',');
      }
      const respons = await this.apiClient.getItemCodeCAM(
        fromDate,
        toDate,
        marketplaceTemplateId,
        division,
        DepartmentIds,
        status,
        planoSearchText,
      );
      const itemCodes = respons?.result?.map((item: any) => item.itemCode);
      if (itemCodes && itemCodes.length > 0) {
        this.setState({ Isdata: true });
        if (this.dropdownOptions.length === 0) {
          const newItem: IDropdownOption = {
            key: 'exportScreen',
            text: this.props.t('Export Current View'),
          };
          this.dropdownOptions.push(newItem);
        }
        const ready = respons?.result;
        if (ready && ready.length > 0) {
          const filteredReady = ready.filter((e: any) => {
            return e.statusfilter?.includes('1');
          });
          if (
            filteredReady.length > 0 &&
            (this.state.DivdeptOption.length != 0 ||
              this.state.fromDate != null ||
              this.state.toDate != null ||
              !(
                this.state.planoSearchText == '' ||
                this.state.planoSearchText == undefined ||
                this.state.planoSearchText == null
              ))
          ) {
            if (this.dropdownOptions.length === 1) {
              const newItem: IDropdownOption = {
                key: 'exportReady',
                text: this.props.t('Export CAM Data'),
              };
              this.dropdownOptions.push(newItem);
            }
          } else {
            this.setState({ downloadenable: false });
          }
        }
      } else {
        this.setState({ downloadenable: false, Isdata: false });
      }

      this.setState({
        searchData2: respons?.result?.map((item: any) => ({
          ...item,
          status: item.statusfilter,
        })),
        searchData1: [itemCodes],
        templateDatas: respons.result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
          status: item.statusfilter,
        })),
        searchData: respons.result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
          status: item.statusfilter,
        })),
        isRecord: false,
      });
    }
    // if (this.commonDateRef.current) {
    //   this.commonDateRef.current.resetDates();
    // }
  }
  private dropdownOptions: IDropdownOption[] = [];
  toggleDropdown = () => {
    this.setState((prevState: any) => ({ isDropdownOpen: !prevState.isDropdownOpen }));
    document.body.addEventListener('click', this.handleOutsideClick);
  };
  closeDropdown = () => {
    this.setState({ isDropdownOpen: false });
    document.body.removeEventListener('click', this.handleOutsideClick);
  };

  handleOutsideClick = (event: any) => {
    const dropdownElement = document.querySelector('.cam-container');
    if (dropdownElement && !dropdownElement.contains(event.target)) {
      this.closeDropdown();
    }
  };
  handleMouseEnter = () => {
    this.setState({ isMouseOverDropdown: true });
  };

  handleMouseLeave = () => {
    this.setState({ isMouseOverDropdown: false, hoveredOption2: false, hoveredOption1: false });
  };

  handleOptionMouseEnter = (option: any) => {
    this.setState({ hoveredOption: option });
  };

  handleOptionMouseLeave = () => {
    this.setState({ hoveredOption: null });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };

  DropdownFilter = async () => {
    const catogoryArray: { key: number; text: string }[] = [];
    const selectedKeys = this.state.deptOption
      .filter((option: any) => this.state.DivdeptOption.includes(option.text))
      .map((option: any) => option.key);
    const filteredOptions = this.state.departmentOption1.filter((option: any) => {
      const parentCodeAsNumber = parseInt(option.division, 10);
      return selectedKeys.includes(parentCodeAsNumber);
    });
    filteredOptions.forEach((filteredOption: any) => {
      catogoryArray.push({ key: filteredOption.id, text: filteredOption.dept_Name });
    }); 
    const catogory = catogoryArray.sort((a, b) => a.text.localeCompare(b.text));
    this.setState({ departmentOption: catogory });
  };

  handlePlanoSearch = async (e: any) => {
    const newValue = (e && e.target && e.target.value) || '';
    this.setState({ planoSearchText: newValue });
  };

  handleMenuItemClick = (ev: any, item: any) => {
    this.setState(
      {
        marketplaceName: item.text,
        marketPlaceId: item.key,
        MRenable: true,
        productDeptId: 0,
        searchKey: '',
        readyAll: false,
        mapingbool: false,
        // templateDatas: [],
      },
      () => {
        this.selectedDivision = 0;
        this.selectedMarketPlace = item;
        // this.refresh(true, null);
      },
    );
    this.setState({
      mapingbool: false,
      MRenable: true,
      searchDivision1: null,
      // templateDatas: [],
      readyAll: false,
      searchKey: '',
      productDeptId1: [],
      refreshKey: this.state.refreshKey + 1,
      refreshKey1: this.state.refreshKey1 + 1,
      statusOptions: 0,
      DivdeptOption: [],
    });
    this.selectedDivision = 0;
    this.selectedMarketPlace = item;
  };

  async searchProducts(statusOptions?: any) {
    var data: any = [];
    var fromDate = this.state.fromDate
      ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
      : undefined;
    var toDate = this.state.toDate
      ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
      : undefined;
    var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
    const status: any = this.state.statusOptions == 0 ? 0 : 1;
    var planoSearchText =
      this.state.planoSearchText == undefined ? null : this.state.planoSearchText;
    let resultString;
    if (this.state.DivdeptOption.length == 0) {
      let filteredData = this.deptId.filter((item) => item !== 0);
      resultString = filteredData.join(',');
    } else {
      resultString = this.state.DivdeptOption.map((divDeptText: string) => {
        const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
        return division ? division.key : null;
      });
    }
    const productDepartmentId: number[] = [];
    this.state.departmentOption
      .filter((row: any) => this.state.productDeptId1.includes(row.text.trim()))
      .forEach((row: any) => {
        const keyAsNumber = Number(row.key);
        if (!isNaN(keyAsNumber) && !productDepartmentId.includes(keyAsNumber)) {
          productDepartmentId.push(keyAsNumber);
        }
      });
    const DepartmentIds: string = productDepartmentId.join(',');
    const respons = (
      await this.apiClient.getItemCodeCAM(
        fromDate,
        toDate,
        marketplaceTemplateId,
        resultString,
        DepartmentIds,
        status,
        planoSearchText,
      )
    ).result;
    respons?.forEach((e: any) => {
      if (data.filter((element: any) => element.itemCode == e.itemCode).length == 0) {
        data.push(e);
      }
    });

    this.setState({
      templateDatas: data?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
        status: item.statusfilter,
      })),
      searchValue: '',
      searchData: data?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
        status: item.statusfilter,
      })),
      searchData2: data?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
        status: item.statusfilter,
      })),
    });
    this.setState({ searchStatus: status });
  }

  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  ItemcodeTemplate = async (event: any, option: any) => {
    try {
      this.setState({ isDropdownOpen: false });
      if (option.text == 'Export CAM Data') {
        if (this.state.searchValue !== '' && this.state.templateDatas.length !== 0) {
          const searchValuesArray = this.state.searchValue.split(',');
          const marketplaceTemplateId =
            this.state.searchAdmindata && this.state.searchAdmindata.length > 0
              ? this.state.searchAdmindata[0].id
              : undefined;

          let url = `${baseUrl}/api/Template/ItemCodeSearchDownload`;

          if (searchValuesArray.length > 0) {
            url += `?itemcode=${searchValuesArray[0]}`;
            for (let i = 1; i < searchValuesArray.length; i++) {
              if (searchValuesArray[i]) {
                url += `&itemcode=${searchValuesArray[i]}`;
              }
            }
          }

          if (marketplaceTemplateId) {
            url += url.includes('?')
              ? `&marketplaceTemplateId=${marketplaceTemplateId}`
              : `?marketplaceTemplateId=${marketplaceTemplateId}`;
          }

          // axios(url, {
          //   method: 'GET',
          //   headers: {
          //     'Content-Type': 'application/json',
          //   },
          // }).then((response: any) => {
          //   if (response.data.hasError === true) {
          //     return this.notify.showErrorNotify(response.data.message);
          //   } else {
              downloadFileItemcodeSearch(url);
            // }
          // });
        } else {
          if (this.state.templateDatas.length !== 0) {
            const fromDate = this.state.fromDate
              ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
              : undefined;
            const toDate = this.state.toDate
              ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
              : undefined;
            var planoSearchText =
              this.state.planoSearchText == undefined ? null : this.state.planoSearchText;
            const searchAdmindata = this.state.searchAdmindata || [];
            const token = this.token;
            let url = `${baseUrl}/api/Template/ItemCodeDownload`;
            url += `?${Object.entries({ token })
              .map(([key, value]) => `${key}=${value}`)
              .join('&')}`;
            searchAdmindata.forEach(async (item: any, index: any) => {
              const marketplaceTemplateId = item.id;
              const Division = item.divisionId;
              const deptcode = item.productDepartmentId;
              var dept = this.state.departmentOption1.find((e: any) => e.id == deptcode)?.dept;
              const matchingTemplate = this.state.templateDatas.find((template: any) => {
                return (
                  template.division == Division && template.deptcode == dept && template.status == 1
                );
              });

              if (matchingTemplate) {
                var filename = this.state.departmentOption1.find(
                  (e: any) => e.id == deptcode,
                )?.dept_Name;
                let url = `${baseUrl}/api/Template/ItemCodeDownload`;
                url += `?${Object.entries({ token })
                  .map(([key, value]) => `${key}=${value}`)
                  .join('&')}`;
                if (planoSearchText) {
                  url += `&planogramName=${planoSearchText}`;
                }
                if (fromDate) {
                  url += `&fromDate=${fromDate}`;
                }
                if (toDate) {
                  url += `&toDate=${toDate}`;
                }
                if (marketplaceTemplateId) {
                  url += `&marketplaceTemplateId=${marketplaceTemplateId}`;
                }
                if (Division) {
                  url += `&division=${Division}`;
                }
                if (dept) {
                  url += `&deptcode=${dept}`;
                }

                try {
                  downloadFileItemcodeReady(url, filename);
                } catch (error: any) {
                  this.notify.showErrorNotify(error);
                }
              }
            });
          }
        }
      } else if (option.text == 'Export Current View') {
        this.CurrentScreenTemplate();
      }
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  SearchClick = () => {
    if (this.state.searchValue == '') {
      this.refresh(true, null);
    } else if (this.state.searchValue != '') {
      this.onSearch();
    }
    // else {
    //   this.refresh(true, null);
    // }
  };
  onSearch1 = (e: any) => {
    const newValue = (e && e.target && e.target.value) || ''; // Handle undefined target value
    this.setState({ searchValue: newValue, searchKey: newValue, readyAll: false }, () => {
      if (this.state.searchData) {
        const result = this.state.searchData
          .filter((element: any) => {
            return (
              element.itemCode &&
              element.itemCode.toLowerCase().includes(newValue.trim().toLowerCase())
              //   ||
              // (element.planogramName &&
              //   element.planogramName.toLowerCase().includes(newValue.trim().toLowerCase()))
            );
          })
          .map((item: any, index: any) => ({
            index: index + 1,
            ...item,
            status: item.statusfilter,
          }));
        this.setState({ templateDatas: result });
      }
    });
  };
  ItemcodeTemplate1 = async () => {
    try {
      const searchAdmindata = this.state.searchAdmindata || [];
      searchAdmindata.forEach(async (item: any, index: any) => {
        const marketplaceTemplateId = item.id;
        try {
          const url = `${baseUrl}/api/Template/MarketPlaceDownload?marketplacetemplateid=${marketplaceTemplateId}`;
          downloadFileMarketPlace(url);
        } catch (error: any) {
          this.notify.showErrorNotify(error);
        }
      });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };
  handleDropdownChange = (option: any) => {
    if (option) {
      this.setState({ searchDivision: option.text });
    }
  };
  areConditionsMet() {
    if (
      this.state.productDeptId !== 0 &&
      this.state.searchDivision !== null &&
      this.selectedDivision !== 0
    ) {
      return true;
    } else {
      return false;
    }
  }
  showAlert = () => {
    this.setState({ showAlert: true });
  };

  hideAlert = () => {
    this.setState({ showAlert: false });
  };
  async onSearch() {
    if (this.state.searchValue != '') {
      const numberRegex = /^[0-9,]+$/;
      if (!numberRegex.test(this.state.searchValue)) {
        this.notify.showErrorNotify('Invalid Itemcode entered.!');
      } else {
        const hasConsecutiveCommas = /,,+/;
        if (hasConsecutiveCommas.test(this.state.searchValue)) {
          this.notify.showErrorNotify('Invalid Pattern Itemcode entered!');
        } else {
          const itemcodesArray = this.state.searchValue
            .split(',')
            .map((code: any) => parseInt(code.trim(), 10));
          const response = await this.apiClient.getItemCodeMultiSearch(itemcodesArray);
          const templateDatas =
            response && response.result
              ? response.result.map((item, index) => ({
                  index: index + 1,
                  dateCreated: item.dateCreated,
                  itemCode: item.itemCode,
                  status: item.statusfilter,
                  statusfilter:item.statusfilter,
                  division: item.division,
                  deptcode: item.deptcode,
                  eancode: item.eancode,
                  uid: item.uid,
                  planogramName: item.planogramName
                }))
              : null;
          this.setState({ templateDatas }, () => {});
        }
      }
    }
    // if (this.state.searchData1 !== undefined && this.state.searchValue !== undefined) {
    //   const searchData1Array = this.state.searchData1[0].split(',');
    //   const searchTerms = this.state.searchValue
    //     .split(',')
    //     .map((term: any) => term.trim().toLowerCase());
    //   const validSearchTerms = searchTerms.filter((term: any) => term.length > 0);
    //   const result = searchData1Array.filter((itemCode: string) => {
    //     return validSearchTerms.some((term: any) => itemCode.toLowerCase() === term);
    //   });
    //   if (result.length !== 0) {
    //     const result1 = this.state.searchData2.filter((item: any) => {
    //       return result.includes(item.itemCode);
    //     });
    //     const templateDatas =
    //       result1.length === 0
    //         ? null
    //         : result1.map((item: any, index: any) => ({
    //             index: index + 1,
    //             dateCreated: item.dateCreated,
    //             itemCode: item.itemCode,
    //             status: item.statusfilter,
    //           }));
    //     this.setState({ templateDatas: templateDatas }, () => {});
    //   } else {
    //     this.setState({ templateDatas: [] });
    //   }
    // } else {
    //   this.setState({ templateDatas: this.state.searchData2 });
    // }
  }
  async FilterFun() {
    const MasterData = (await this.apiClient.getProductdeptMasters()).result;
    const departmentSet = new Set();
    const departmentMap = new Map();

    MasterData?.forEach((element) => {
      if (this.deptId.includes(element.division)) {
        const deptdata = {
          dept_Name: element.name,
          dept: element.code,
          division: element.division,
          id: element.id,
        };
        const deptdropdown = {
          dept_Name: element.name,
          id: element.id,
          dept: element.code,
        };
        const key = `${deptdata.dept_Name}_${deptdata.dept}_${deptdata.division}`;
        let isUnique = true;
        departmentMap.forEach((existingData: any) => {
          if (
            existingData.dept_Name === deptdata.dept_Name &&
            existingData.dept === deptdata.dept &&
            existingData.division === deptdata.division &&
            existingData.id === deptdata.id
          ) {
            isUnique = false;
          }
        });
        if (isUnique) {
          departmentMap.set(key, deptdata);
          departmentSet.add(deptdropdown);
        }
      }
    });
    // const departmentList = Array.from(departmentSet).map((deptName) => ({
    //   text: deptName,
    // }));
    const departmentList = Array.from(departmentSet)
      .map((deptName: any) => ({
        text: deptName.dept_Name,
        key: deptName.dept,
      }))
      .sort((a: any, b: any) => {
        return a.text.localeCompare(b.text);
      });
    this.setState({
      departmentOption: departmentList,
      departmentOption1: Array.from(departmentMap.values()),
    });
  }
  openSearchModal = () => {
    this.setState({ isSearchModalOpen: true });
  };

  closeSearchModal = () => {
    this.setState({ isSearchModalOpen: false });
  };
  Mappingcheck = () => {
    if (this.state.templateDatas != undefined) {
      if (
        this.state.mapingbool &&
        this.state.templateDatas.length != 0 &&
        this.state.DivdeptOption.length !== 0 &&
        this.state.productDeptId1.length !== 0
      ) {
        return true;
      } else {
        return false;
      }
    } else {
      return false;
    }
  };
  onChange = (ev: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) => {
    this.setState({
      selectedDepartment: option.key,
      selectedDivisionId: option.key,
      mapingbool: false,
      productDeptId: 0,
      searchKey: '',
    });
    var filteredDivision: any = [];
    filteredDivision.push({ key: option.key, text: option.text });
    var deptOpt: any = [];
    const filteredDivOpt1 = this.state.departmentOption1?.filter((department: any) => {
      return filteredDivision.some(
        (filter: any) => department.parentCode === filter.key.toString(),
      );
    });
    filteredDivOpt1?.forEach((element: any) => {
      deptOpt.push({ key: element.key, text: element.text });
    });
    this.setState({ departmentOption: deptOpt });
    this.selectedDivision = option.key;
  };
  CurrentScreenTemplate = async () => {
    // const divisionId = this.state.DivdeptOption;
    // var productId1 = this.state.productDeptId1;
    // if (
    //   productId1 == null ||
    //   productId1.length == 0 ||
    //   divisionId == null ||
    //   divisionId.length == 0
    // ) {
    //   // this.notify.showErrorNotify(this.props.t('PLEASE_SELECT_MARKETPLACE,_DIVISION_AND_CATEGORY'));
    //   this.notify.showErrorNotify(this.props.t('Download will not work today...!! Stay tuned!!'));
    //   // this.notify.showErrorNotify(
    //   //   this.props.t('Please select Market place,Division and Category'),
    //   // );
    //   this.setState({
    //     marketPlaceId: this.state.marketPlaceId,
    //     templateDatas: [],
    //     isRecord: false,
    //     searchKey: '',
    //   });
    //   return;
    // } else
    let resultString: any;
    let productDeptId: any;
    if (this.state.DivdeptOption.length == 0) {
      let filteredData = this.deptId.filter((item) => item !== 0);
      resultString = filteredData.join(',');
    } else {
      resultString = this.state.DivdeptOption.map((divDeptText: string) => {
        const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
        return division ? division.key : null;
      });
    }
    if (this.state.productDeptId1.length != 0 && this.state.productDeptId1 != null) {
      const deptkey = this.state.productDeptId1.map((DeptText: string) => {
        const depetname = this.state.departmentOption1.find((e: any) => e.dept_Name === DeptText);
        return depetname ? depetname.dept : null;
      });
      productDeptId = deptkey.join(',');
    }
    var planoSearchText =
      this.state.planoSearchText == undefined ? null : this.state.planoSearchText;
    const status = this.state.statusOptions;
    if (this.state.templateDatas != undefined) {
      const fromDate = this.state.fromDate
        ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
        : undefined;
      const toDate = this.state.toDate
        ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
        : undefined;
      if (this.state.productDeptId1.length != 0) {
        const searchAdmindata = this.state.searchAdmindata || [];
        const token = this.token;
        searchAdmindata.forEach(async (item: any, index: any) => {
          const marketplaceTemplateId = item.id;
          let url = `${baseUrl}/api/Template/ItemcodeAllDownload?`;
          url += `?${Object.entries({ token })
            .map(([key, value]) => `${key}=${value}`)
            .join('&')}`;
          if (marketplaceTemplateId) {
            url += `&marketplaceTemplateId=${marketplaceTemplateId}`;
          }
          if (planoSearchText) {
            url += `&planogramName=${planoSearchText}`;
          }
          if (fromDate) {
            url += `&fromDate=${fromDate}`;
          }
          if (toDate) {
            url += `&toDate=${toDate}`;
          }
          if (resultString) {
            url += `&division=${resultString}`;
          }

          if (productDeptId) {
            url += `&deptcode=${productDeptId}`;
          }
          if (status == 0 || status) {
            url += `&status=${status}`;
          }

          try {
            downloadFileItemcodeAll(url);
          } catch (error: any) {
            this.notify.showErrorNotify(error);
          }
        });
      } else {
        const marketplaceTemplateId = this.state.searchAdmindata[0].id;
        const token = this.token;
        let url = `${baseUrl}/api/Template/ItemcodeAllDownload?`;
        url += `?${Object.entries({ token })
          .map(([key, value]) => `${key}=${value}`)
          .join('&')}`;
        if (marketplaceTemplateId) {
          url += `&marketplaceTemplateId=${marketplaceTemplateId}`;
        }
        if (planoSearchText) {
          url += `&planogramName=${planoSearchText}`;
        }
        if (fromDate) {
          url += `&fromDate=${fromDate}`;
        }
        if (toDate) {
          url += `&toDate=${toDate}`;
        }
        if (resultString) {
          url += `&division=${resultString}`;
        }
        if (productDeptId) {
          url += `&deptcode=${productDeptId}`;
        }
        if (status == 0 || status) {
          url += `&status=${status}`;
        }
        try {
          downloadFileItemcode(url);
        } catch (error: any) {
          this.notify.showErrorNotify(error);
        }
      }
    } else {
      this.notify.showErrorNotify(this.props.t('NO_DATA_TO_DOWNLOAD'));
    }
  };
  render() {
    const { isDropdownOpen, hoveredOption } = this.state;
    const { t } = this.props;
    const allCategorySelected =
      this.state.departmentOption && this.state.departmentOption.length
        ? this.state.productDeptId1.length === this.state.departmentOption.length
        : false;
    const allDivSelected =
      this.state.deptOption && this.state.deptOption.length
        ? this.state.DivdeptOption &&
          this.state.DivdeptOption.length === this.state.deptOption.length
        : false;
    const column: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '80px',
      },

      {
        key: 'uid',
        name: <div className={columnHeader}>{`${t('COL_UID')}`}</div>,
        selector: (row: any) => <span title={row.uid}>{row.uid}</span>,
        isVisible: true,
        width: '150px',
      },
      {
        key: 'planogramName',
        name: <div className={columnHeader}>{`${t('PLANOGRAM_TABLE_PLANO/FAMILY_NAME')}`}</div>,
        selector: (row: any) => <span title={row.planogramName}>{row.planogramName}</span>,
        isVisible: true,
        width: '150px',
      },
      {
        key: 'itemCode',
        name: <div className={columnHeader}>{`${t('ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        isVisible: true,
        width: '150px',
      },
      {
        key: 'eancode',
        name: <div className={columnHeader}>{`${t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eancode}>{row.eancode}</span>,
        isVisible: true,
        width: '150px',
      },
      {
        key: 'col_divCode',
        name: <div className={columnHeader}>{`${t('DIVISION')}`}</div>,
        selector: (row: any) => (
          <span title={this.state.divisionOption.find((e: any) => e.key == row.division)?.text}>
            {this.state.divisionOption.find((e: any) => e.key == row.division)?.text}
          </span>
        ),
        isVisible: true,
        width: '150px',
      },
      {
        key: 'dept_code',
        name: <div className={columnHeader}>{`${t('COL_DEPARTMENT')}`}</div>,
        selector: (row: any) => (
          <span
            title={this.state.departmentOption1.find((e: any) => e.dept == row.deptcode)?.dept_Name}
          >
            {this.state.departmentOption1.find((e: any) => e.dept == row.deptcode)?.dept_Name}
          </span>
        ),
        isVisible: true,
        width: '180px',
      },
      {
        key: 'dateCreated',
        name: <div className={columnHeader}>{`${t('DATE_CREATED')}`}</div>,
        selector: (row: any) => (
          <span
            title={
              row.dateCreated != null && row.dateCreated != ''
                ? new Date(row.dateCreated).toLocaleDateString('en-GB')
                : ''
            }
          >
            {row.dateCreated != null && row.dateCreated != ''
              ? new Date(row.dateCreated).toLocaleDateString('en-GB')
              : ''}
          </span>
        ),
        isVisible: true,
        width: '140px',
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${t('COL_STATUS')}`}</div>,
        selector: (row: any) => {
          if (row && Object.values(row).some((value) => value !== '')) {
            const status =
              row.statusfilter && row.statusfilter.length > 0 ? row.statusfilter[0] : null;
            const statusText =
              status != null ? (status === '1' ? t('READY') : t('NOT_READY')) : t('NOT_READY');
            return <span title={statusText}>{statusText}</span>;
          }
        },
        isVisible: true,
        maxWidth: '250px',
      },
    ];
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...column];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    const menuProps: IContextualMenuProps = {
      shouldFocusOnMount: true,
      items: this.state.templateDropdownOption.map((item: any) => ({
        ...item,
        onClick: this.handleMenuItemClick,
      })),
    };
    const selection = () => {
      if (this.state.templateDatas == undefined) {
        return false;
      } else {
        if (
          (this.state.templateDatas.length !== 0 &&
            this.state.statusOptions !== 0 &&
            this.state.Download === true) ||
          (this.state.searchValue != '' && this.state.templateDatas.length !== 0)
        ) {
          let allRecordsHaveStatusOne = false;
          if (
            (this.state.statusOptions !== 0 || this.state.DivdeptOption.length != 0) &&
            this.state.searchValue == ''
          ) {
            allRecordsHaveStatusOne = this.state.templateDatas.every((item: any) => {
              return item.status == 1;
            });
            return allRecordsHaveStatusOne;
          } else if (this.state.searchValue != '' && this.state.templateDatas.length !== 0) {
            return (allRecordsHaveStatusOne = true);
          }
          return allRecordsHaveStatusOne;
        } else {
          return false;
        }
      }
    };
    return (
      <>
        <div className="layout width-100">
          <Stack>
            {/* <Text block className="text">{`${t('PIM')} / ${t('OUTPUT')} / ${t('TEMPLATES')}`}</Text> */}
            <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
              'CAM_DATA',
            )}`}</Text>
          </Stack>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={5.3}>
              <DefaultButton
                className="button"
                text={t('HOMECENTRE')}
                styles={this.state.marketPlaceId == 1 ? PIMButtons : PIMHeaderButtons}
                onClick={() => {
                  this.setState({
                    marketPlaceId: 1,
                    mapingbool: false,
                    MRenable: true,
                    marketplaceName: '-Select marketplace-',
                    searchDivision1: null,
                    // templateDatas: [],
                    readyAll: false,
                    searchKey: '',
                    productDeptId1: [],
                    refreshKey: this.state.refreshKey + 1,
                    refreshKey1: this.state.refreshKey1 + 1,
                    statusOptions: 0,
                    previousStatusOptions: 0,
                    DivdeptOption: [],
                  });
                }}
              />
            </Grid>
            <Grid item lg={2} xs={5.3}>
              <DefaultButton
                className="button"
                text={this.state.marketplaceName}
                styles={this.state.marketPlaceId != 1 ? PIMButtons : PIMHeaderButtons}
                menuProps={menuProps}
              />
            </Grid>
            <Grid item lg={2} xs={5.3}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('FAMILY_NAME')}
                onChange={this.handlePlanoSearch}
                value={this.state.planoSearchText}
                onClear={() => this.setState({ planoSearchText: '' })}
              />
            </Grid>
            <Grid item lg={5.4} xs={0} />
            {/* <Grid item lg={0.5} xs={1}>
              <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle"
                onClick={() => {
                  if (selection()) {
                    this.ItemcodeTemplate();
                  } else {
                    this.CurrentScreenTemplate();
                  }
                }}
              />
            </Grid> */}
            <Grid item lg={0.5} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={this.toggleDropdown}
              />

              {isDropdownOpen && (
                <div
                  className={mergeStyles('cam-container', {
                    position: 'absolute',
                    top: '52px',
                    right: '44px',
                    height: '1px',
                    padding: '5px 0px',
                    zIndex: 9999,
                    backgroundColor: 'rgb(210, 238, 234)',
                    borderRadius: '5px',
                    textAlign: 'left',
                  })}
                  onMouseEnter={this.handleMouseEnter}
                  onMouseLeave={this.handleMouseLeave}
                  style={{
                    height: 'auto',
                    padding: '5px 0px',
                  }}
                >
                  <DefaultButton
                    onClick={() => {
                      this.ItemcodeTemplate(null, { text: 'Export Current View' });
                      this.setState({
                        hoveredOption: null,
                        isDropdownOpen: false,
                        hoveredOption1: false,
                      });
                    }}
                    disabled={this.state.Isdata == false}
                    style={{
                      fontFamily: 'Montserrat, sans-serif',
                      padding: '8px 0px 12px',
                      fontSize: '14px',
                      cursor: 'pointer',
                      border: 'none',
                      height: '1px',
                      backgroundColor: this.state.hoveredOption1 ? 'lightgray' : 'transparent',
                      display: 'block',
                      width: '100%',
                      fontWeight: 'bold',
                    }}
                    onMouseEnter={() => {
                      this.setState({ hoveredOption1: true });
                    }}
                    onMouseLeave={() => {
                      this.setState({ hoveredOption1: false });
                    }}
                  >
                    <span className="CAMspan"> Export Current View</span>
                  </DefaultButton>
                  <DefaultButton
                    onClick={() => {
                      this.ItemcodeTemplate(null, { text: 'Export CAM Data' });
                      this.setState({
                        hoveredOption: null,
                        isDropdownOpen: false,
                        hoveredOption2: false,
                      });
                    }}
                    disabled={this.state.downloadenable === false}
                    style={{
                      fontFamily: 'Montserrat, sans-serif',
                      padding: '8px 0px 12px',
                      fontSize: '14px',
                      cursor: 'pointer',
                      border: 'none',
                      height: '12px',
                      backgroundColor: this.state.hoveredOption2 ? 'lightgray' : 'transparent',
                      display: 'block',
                      width: '100%',
                      fontWeight: 'bold',
                    }}
                    onMouseEnter={() => {
                      this.setState({ hoveredOption2: true });
                    }}
                    onMouseLeave={() => {
                      this.setState({ hoveredOption2: false });
                    }}
                  >
                    <span className="CAMspanbutton"> Export CAM Data </span>
                  </DefaultButton>
                </div>
              )}
            </Grid>
          </Grid>
          <div className="Catalog"></div>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={6}>
              {/* <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('ITEMCODE')}
                onClick={(e: any) => {
                  this.onSearch1(e);
                  this.setState({ searchKey: e?.target.value });
                  // this.setState({ Searchenable: true });
                }}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              /> */}
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('ITEMCODE')}
                onChange={this.onSearch1}
                value={this.state.searchValue}
                onClear={() => this.setState({ searchValue: '' })}
              />
            </Grid>

            <Grid item lg={2} xs={6}>
              <ComboBox
                className="dropdown"
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allDivSelected },
                  ...(this.state.deptOption || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.DivdeptOption)
                      ? this.state.DivdeptOption.includes(option.text)
                      : false,
                  })),
                ]}
                styles={PIMdropdownStyles2}
                key={this.state.refreshKey1}
                multiSelect
                selectedKey={this.state.DivdeptOption}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    productDeptId1: [],
                    refreshKey: this.state.refreshKey + 1,
                  });
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected =
                      (this.state.DivdeptOption || []).length ===
                      (this.state.deptOption || []).length;
                    const newSelectedSeasons = allSelected
                      ? []
                      : (this.state.deptOption || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);
                    this.setState({ DivdeptOption: newSelectedSeasons }, () => {
                      this.setState(
                        {
                          productDeptId: selectedOption.key,
                          // selectedOption,
                          searchKey: '',
                          searchValue: '',
                        },
                        () => {
                          this.DropdownFilter();
                        },
                      );
                    });
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.DivdeptOption)
                      ? [...this.state.DivdeptOption]
                      : [];
                    const isSelected = Array.isArray(this.state.DivdeptOption)
                      ? this.state.DivdeptOption.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                    }
                    this.setState(
                      {
                        DivdeptOption: updatedSelectedSeasons,
                        productDeptId: selectedOption.key,
                        // selectedOption,
                        searchKey: '',
                        searchValue: '',
                      },
                      () => {
                        this.DropdownFilter();
                      },
                    );
                  }
                }}
                placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                // style={{ marginTop: -2 }}
              />
            </Grid>
            <Grid item lg={2} xs={6}>
              <ComboBox
                className="dropdown"
                multiSelect
                disabled={this.state.DivdeptOption.length == 0}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allCategorySelected },
                  ...(this.state.departmentOption || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.productDeptId1)
                      ? this.state.productDeptId1.includes(option.text)
                      : false,
                  })),
                ]}
                key={this.state.refreshKey}
                styles={productDropdown}
                placeholder={`${t('DROPDOWN_SELECT_CATEGORY')}`}
                selectedKey={this.state.productDeptId1 || []}
                onChange={(e: any, selectedOption: any) => {
                  this.setState(
                    {
                      //  productDeptId: selectedOption.key,
                      // selectedOption,
                      searchKey: '',
                      searchValue: '',
                      mapingbool: true,
                    },
                    () => {
                      // this.refresh(true, selectedOption.key);
                    },
                  );
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected =
                      (this.state.productDeptId1 || []).length ===
                      (this.state.departmentOption || []).length;
                    const newSelectedSeasons = allSelected
                      ? []
                      : (this.state.departmentOption || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);
                    this.setState({ productDeptId1: newSelectedSeasons }, () => {
                      this.setState(
                        {
                          // productDeptId: selectedOption.key,
                          // selectedOption,
                          searchKey: '',
                          searchValue: '',
                          mapingbool: true,
                        },
                        () => {
                          // this.refresh(true, this.state.productDeptId1);
                        },
                      );
                    });
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.productDeptId1)
                      ? [...this.state.productDeptId1]
                      : [];
                    const isSelected = Array.isArray(this.state.productDeptId1)
                      ? this.state.productDeptId1.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                    }
                    this.setState(
                      {
                        productDeptId1: updatedSelectedSeasons,
                        // productDeptId: selectedOption.key,
                        // selectedOption,
                        searchKey: '',
                        searchValue: '',
                        mapingbool: true,
                      },
                      () => {
                        // this.refresh(true, this.state.productDeptId1);
                      },
                    );
                  }
                }}
              />
            </Grid>
            <Grid item lg={1.9} xs={6}>
              {/* {this.state.productDeptId1 == null || this.state.productDeptId1 == 0 ? (
                <TextField
                  placeholder={`${t('PLACEHOLDER_FROM_DATE')} - ${t('PLACEHOLDER_TO_DATE')}`}
                  // disabled={true}
                  iconProps={{ iconName: 'Calendar' }}
                />
              ) : ( */}
              <CommonDate
                onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                onToDateChange={(e: any) => this.handleToDateChange(e)}
                label={`${t('ITEMCREATION_DATE')}`}
                ref={this.commonDateRef}
              />
              {/* )} */}
            </Grid>
            <Grid item lg={1.2} xs={6}>
              <Dropdown
                className="dropdown"
                placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                options={this.statusOptions}
                styles={productDropdown}
                key={this.state.refreshKey}
                defaultSelectedKey={this.state.statusOptions}
                selectedKey={this.state.statusOptions}
                onChanged={(selectedOption: IDropdownOption) => {
                  if (selectedOption.key !== 0) {
                    this.setState({
                      previousStatusOptions: selectedOption.key,
                    });
                  }
                  this.setState({
                    statusOptions: selectedOption.key,
                    readyAll: true,
                    searchKey: '',
                    searchValue: '',
                  });
                }}
                // disabled={this.state.productDeptId1 == 0 || this.state.productDeptId1 == null}
              />
            </Grid>
            <Grid item lg={1.4} xs={6}>
              <DefaultButton
                styles={PIMButtons}
                text={t('VIEW_MAPPING')}
                disabled={!this.Mappingcheck()}
                iconProps={{ iconName: 'Download' }}
                onClick={() => {
                  this.ItemcodeTemplate1();
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={2}>
              <Icon
                iconName="Search"
                title={t('BTN_SEARCH')}
                className="iconStyle"
                style={{ marginTop: '1px' }}
                onClick={() => {
                  if (this.state.statusOptions === 0) {
                    this.setState({
                      previousStatusOptions: this.state.statusOptions,
                    });
                  }
                  if (
                    this.state.DivdeptOption.length != 0 ||
                    (this.state.statusOptions.length != 0 &&
                      this.state.previousStatusOptions != 0) ||
                    this.state.fromDate != null ||
                    this.state.toDate != null ||
                    this.state.marketPlaceId != 1 ||
                    !(
                      this.state.planoSearchText == '' ||
                      this.state.planoSearchText == undefined ||
                      this.state.planoSearchText == null
                    )
                  ) {
                    this.SearchClick();
                    this.setState({ downloadenable: true });
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.4} xs={2}>
              <Icon
                iconName="Clear"
                title={t('BTN_CLEAR')}
                style={{ marginTop: '1px' }}
                className="iconStyle"
                onClick={() => {
                  if (
                    this.state.DivdeptOption.length != 0 ||
                    (this.state.statusOptions.length != 0 && this.state.statusOptions != 0) ||
                    this.state.fromDate != null ||
                    this.state.toDate != null ||
                    this.state.marketPlaceId != 1 ||
                    this.state.searchKey != '' ||
                    this.state.searchValue != '' ||
                    this.state.planoSearchText != ''
                  ) {
                    this.setState({
                      DivdeptOption: [],
                      productDeptId1: [],
                      searchDivision1: null,
                      templateDatas: [],
                      searchKey: '',
                      searchValue: '',
                      fromDate: null,
                      toDate: null,
                      statusOptions: 0,
                      marketPlaceId: 1,
                      previousStatusOptions: 0,
                      marketplaceName: '-Select marketplace-',
                      readyAll: false,
                      planoSearchText: '',
                      downloadenable: false,
                    });
                    this.selectedDivision = 0;
                    // this.componentDidMount();
                    this.refresh(true, null);
                    this.ClearDates();
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={2}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                style={{ marginTop: '1px' }}
                className="iconStyle"
                onClick={() => {
                  this.setState(
                    (prevState: any) => ({
                      MRenable: prevState.MRenable === false,
                      marketplaceName: '-Select marketplace-',
                      productDeptId: 0,
                      searchDivision1: null,
                      templateDatas: [],
                      searchData: [],
                      searchData2: [],
                      searchKey: '',
                      marketPlaceId: 1,
                      fromDate: null,
                      toDate: null,
                      statusOptions: 0,
                      readyAll: false,
                      searchValue: '',
                      DivdeptOption: [],
                      productDeptId1: [],
                      planoSearchText: '',
                      downloadenable: false,
                    }),
                    () => {
                      this.selectedDivision = 0;
                      this.refresh(true, null);
                    },
                  );
                  this.selectedDivision = 0;
                  // this.componentDidMount();
                  this.ClearDates();
                }}
              />
            </Grid>
            <Modal
              isOpen={this.state.Searchenable}
              containerClassName={PIMcontentStyles.confirmContainer1}
              isBlocking={false}
              onDismiss={() =>
                this.setState({
                  Searchenable: false,
                  searchKey1: '',
                })
              }
            >
              <div className={PIMcontentStyles.confirmbody1}>
                <Text variant="xLarge" className="apptext123">{`${this.props.t(
                  'ITEM_CODE_SEARCH',
                )}`}</Text>
              </div>
              <SearchBox
                className="Search"
                styles={PIMsearchBoxStyles123}
                placeholder={`${t('ITEMCODE')}`}
                onChange={(e: any) => {
                  this.setState(
                    {
                      templateDatas: [],
                      searchDivision1: null,
                      fromDate: null,
                      toDate: null,
                      productDeptId: 0,
                      marketPlaceId: 0,
                      marketplaceName: '-Select marketplace-',
                      // readyAll: false,
                    },
                    () => {
                      this.selectedDivision = 0;
                    },
                  );
                  const newValue = e?.target?.value || '';
                  this.setState({ searchValue1: newValue, searchKey1: newValue });
                }}
                value={this.state.searchKey1}
                onClear={() => this.setState({ searchKey1: '' })}
              />
              <div className={PIMcontentStyles.footer}>
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  <Grid item lg={3.3} xs={5.3} />
                  <Grid item lg={2} xs={5.3}>
                    <DefaultButton
                      disabled={this.state.searchKey1 == ''}
                      styles={PIMButtons123}
                      onClick={() => {
                        this.onSearch();
                      }}
                    >
                      Search
                    </DefaultButton>
                  </Grid>
                  <Grid item lg={0.5} xs={5.3} />
                  <Grid item lg={2} xs={5.3}>
                    <DefaultButton
                      styles={PIMButtons123}
                      onClick={() => this.setState({ Searchenable: false, searchKey1: '' })}
                    >
                      Cancel
                    </DefaultButton>
                  </Grid>
                </Grid>
              </div>
            </Modal>
          </Grid>
          <div className="dataTableFixedFooter">
            <DataTable
              className="CAMdatatable"
              columns={column}
              data={this.state.templateDatas}
              onColumnOrderChange={handleColumnReorder}
              pagination={true}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeaderScrollHeight="63vh"
              fixedHeader
              striped
              paginationComponentOptions={{ rowsPerPageText: `${t('ROWS_PER_PAGE')}` }}
              paginationPerPage={this.Defaultperpage()}
              paginationRowsPerPageOptions={this.perpage()}
              noDataComponent={
                <div className="noDataWidth">
                  {<DataTable columns={column} data={[{ '': '' }]} />}
                  <Stack className="noRecordsWidth">
                    {this.state.isRecord == true ? `${t('RECORDS')}` : `${t('NO_RECORDS')}`}
                  </Stack>
                </div>
              }
            />
          </div>
        </div>
      </>
    );
  }
}
const ComponentTranslated = withTranslation()(MarketTemplate);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
