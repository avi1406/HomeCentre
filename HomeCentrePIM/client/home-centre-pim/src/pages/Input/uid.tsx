import {
  DefaultButton,
  SearchBox,
  Stack,
  Image,
  Modal,
  IconButton,
  DatePicker,
  Dropdown,
  TooltipHost,
  Icon,
  IDropdownOption,
  IButtonProps,
  mergeStyles,
  ComboBox,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import {
  columnHeader,
  PIMButtons,
  PIMcontentStyles,
  PIMDropdownStyles,
  PIMDropdownStyles1,
  PIMDropdownStyles11,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import DataTable from 'react-data-table-component';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import { cancelIcon, iconButtonStyles,DeleteIcon } from '@/common/properties';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import { downloadFileUID, downloadImageUID } from '@/pages/commonFunctions';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import AuthManagerService from '@/services/AuthManagerService';
import { Switch } from '@fluentui/react-components';
import { RowState } from 'react-data-table-component/dist/src/DataTable/types';
import { AttributesExportModel } from '@/services/ApiClient';

class UID extends React.Component<any, any> {
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token: any = sessionStorage.getItem('token');
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  roleId: any[] = this.user?.roleId || [];
  departmentId: any[] = this.user?.departmentId || [];
  selectedSeason1: any = [];
  selectedDeptno1: any = [];
  Error: any;
  selectedClassCode1: any = [];
  selectedSubClassCode1: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      uidData: [],
      searchData: [],
      isRecord: false,
      searchKey: '',
      pictureData: [],
      imageURL: '',
      fromDate: null,
      toDate: null,
      imgVisible: false,
      imageLoading: false,
      downloadFromDate: null,
      downloadToDate: null,
      selectedSeason1: [],
      selectedDeptno1: [],
      selectedClassCode1: [],
      selectedSubClassCode1: [],
      refreshKey: 0,
      refreshKey1: 0,
      refreshKey2: 0,
      exportvisible: false,
      searchval: '',
      isDropdownOpen: false,
      selectedOption: '',
      confirmVisible: false,
      isMouseOverDropdown: false,
      dynamicDropdownOptions: [],
      imageDownload: '',
      ClassCodeListData1: [],
      Sub_class_codeListData1: [],
      departmentListData1: [],
      selectedSeasons: [],
      selecteddepartment: [],
      selectedclass: [],
      selectedsubclass: [],
      seasonDownload: [],
      imageId:null,
      isUidActive: [],
      isUidActiveModalOpen: false,
      uidInModal: "",
      isActiveInModal: false
    };
  }

  commonDateRef = React.createRef<CommonDateRef>();

  async componentDidMount() {
    this.FilterFun();
    this.refresh(this.state.fromDate, this.state.toDate);
  }

  async refresh(startDate: any, endDate: any) {
    this.setState({
      isRecord: true,
      searchKey: '',
      selectedclass: [],
      selectedsubclass: [],
      selecteddepartment: [],
      selectedSeasons: [],
      refreshKey: this.state.refreshKey + 1,
      refreshKey1: this.state.refreshKey1 + 1,
      refreshKey2: this.state.refreshKey2 + 1,
    });
    var data: any = [];
    const fromDate = startDate?.toLocaleDateString('en-CA') || '';
    const toDate = endDate?.toLocaleDateString('en-CA') || '';
    const response = (await this.apiClient.searchUidProducts(fromDate, toDate)).result;
    if (response?.length !== 0) {
      const newItemAll: IDropdownOption = {
        key: 'exportAll',
        text: this.props.t('All Attributes Insights'),
      };
      const newItem: IDropdownOption = {
        key: 'exportScreen',
        text: this.props.t('UID Attributes Insights'),
      };
      const newItemT1: IDropdownOption = {
        key: 'exportT1',
        text: this.props.t('T1 Attributes Insights'),
      };
      const newItemT2: IDropdownOption = {
        key: 'exportT2',
        text: this.props.t('T2 Attributes Insights'),
      };
      const newItemQC: IDropdownOption = {
        key: 'exportQC',
        text: this.props.t('QC Attributes Insights'),
      };
      const newItemPDT: IDropdownOption = {
        key: 'exportPDT',
        text: this.props.t('Packaging Attributes Insights'),
      };
      const exists = this.dropdownOptions.some(
        (option) => option.key === newItem.key && option.text === newItem.text,
      );
      const existsAll = this.dropdownOptions.some(
        (option) => option.key === newItemAll.key && option.text === newItemAll.text,
      );
      const existsT1 = this.dropdownOptions.some(
        (option) => option.key === newItemT1.key && option.text === newItemT1.text,
      );
      const existsT2 = this.dropdownOptions.some(
        (option) => option.key === newItemT2.key && option.text === newItemT2.text,
      );
      const existsQC = this.dropdownOptions.some(
        (option) => option.key === newItemQC.key && option.text === newItemQC.text,
      );
      const existsPDT = this.dropdownOptions.some(
        (option) => option.key === newItemPDT.key && option.text === newItemPDT.text,
      );

      if (!exists) {
        this.dropdownOptions.push(newItem);
      }
      if (!existsT1){
        this.dropdownOptions.push(newItemT1);
      }
      if (!existsT2){
        this.dropdownOptions.push(newItemT2);
      }
      if (!existsQC){
        this.dropdownOptions.push(newItemQC);
      }
      if (!existsPDT){
        this.dropdownOptions.push(newItemPDT);
      }
      if (!existsAll) {
        this.dropdownOptions.push(newItemAll);
      }
    } else {
      if (this.dropdownOptions.length === 2 && response?.length == 0) {
        this.dropdownOptions = this.dropdownOptions.slice(0, 1);
      } else {
        this.dropdownOptions = this.dropdownOptions.slice(0, 1);
      }
      if (this.dropdownOptions.length > 0) {
        this.dropdownOptions = [];
      }
    }
    const picData = (await this.apiClient.getFileNameAndUrl()).result;
    let responseDataArray;
    if (response) {
      responseDataArray = Object.values(response);
    }
    this.setState({
      uidData: this.roleId.includes(1) ? responseDataArray?.map((item: any, index: any) => ({ index: index + 1, ...item })) :
      responseDataArray?.filter((data:any)=> data.isactive == true).map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchData: this.roleId.includes(1) ? responseDataArray?.map((item: any, index: any) => ({ index: index + 1, ...item })) :
      responseDataArray?.filter((data:any)=> data.isactive == true).map((item: any, index: any) => ({ index: index + 1, ...item })),
      pictureData: picData,
      isRecord: false,
      fromDate: startDate,
      toDate: endDate,
      downloadFromDate: startDate,
      downloadToDate: endDate,
      exportvisible: data.length !== 0 ? true : false,
    });
    this.selectedSeason1 = [];
    this.selectedDeptno1 = [];
    this.selectedClassCode1 = [];
    this.selectedSubClassCode1 = [];
    this.setState({
      uidData: this.roleId.includes(1) ? responseDataArray?.map((item: any, index: any) => ({ index: index + 1, ...item })) :
      responseDataArray?.filter((data:any)=> data.isactive == true).map((item: any, index: any) => ({ index: index + 1, ...item })),
      selectedSeason1: [],
      selectedDeptno1: [],
      selectedClassCode1: [],
      selectedSubClassCode1: [],
      refreshKey: this.state.refreshKey + 1,
      refreshKey1: this.state.refreshKey1 + 1,
      refreshKey2: this.state.refreshKey2 + 1,
    });
  }
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  async FilterFun() {
    const MasterData = (await this.apiClient.getHierarchicalMasters()).result;
    const departmentSet = new Set();
    const subClassSet = new Set();
    const classCodeSet = new Set();
    const departmentSet1 = new Set();
    const classCodeSet1 = new Set();
    const subClassSet1 = new Set();

    MasterData?.forEach((element) => {
      if (this.departmentId.includes(element.division)) {
        departmentSet.add(element.dept_Name);
      }
      if (this.departmentId.includes(element.division)) {
        classCodeSet.add(element.class_Name);
      }
      if (this.departmentId.includes(element.division)) {
        subClassSet.add(element.sub_Name);
      }
      if (this.departmentId.includes(element.division)) {
        const deptdata = {
          dept_Name: element.dept_Name,
          dept: element.dept,
          class_Name: element.class_Name,
          class: element.class,
          sub_Name: element.sub_Name,
          subclass: element.subclass,
        };
        departmentSet1.add(deptdata);
      }
      if (this.departmentId.includes(element.division)) {
        const classdata = { class_Name: element.class_Name, class: element.class };
        classCodeSet1.add(classdata);
      }
      if (this.departmentId.includes(element.division)) {
        const subclassdata = { sub_Name: element.sub_Name, subclass: element.subclass };
        subClassSet1.add(subclassdata);
      }
    });

    const departmentList = Array.from(departmentSet).map((deptName) => ({
      text: deptName,
    }));
    const subClassCodeList = Array.from(subClassSet).map((subName) => ({
      text: subName,
    }));
    const classCodeList = Array.from(classCodeSet).map((className) => ({
      text: className,
    }));

    const SeasonsData = await this.apiClient.getSeasons();
    const SeasonList = SeasonsData?.result?.map((element: any) => ({
      text: element.name,
    }));
    const SeasonList1 = SeasonsData?.result?.map((element: any) => ({
      text: element.name,
      key: element.code,
    }));

    this.setState({
      SeasonListData: SeasonList,
      departmentListData: departmentList,
      Sub_class_codeListData: subClassCodeList,
      ClassCodeListData: classCodeList,

      departmentListData1: Array.from(departmentSet1),
      Sub_class_codeListData1: Array.from(subClassSet1),
      ClassCodeListData1: Array.from(classCodeSet1),
      seasonDownload: SeasonList1,
    });
  }

  DropdownFilter = async () => {
    const subClassSet = new Set();
    const classCodeSet = new Set();

    this.state.departmentListData1?.forEach((entry: any) => {
      if (
        this.state.selecteddepartment.includes(entry.dept_Name) &&
        this.state.selectedclass.length === 0
      ) {
        classCodeSet.add(entry.class_Name);
      }

      if (
        this.state.selecteddepartment.includes(entry.dept_Name) &&
        this.state.selectedclass.includes(entry.class_Name) &&
        this.state.selectedclass.length != 0
      ) {
        subClassSet.add(entry.sub_Name);
      }
    });

    const classCodeList = Array.from(classCodeSet).map((className) => ({
      text: className,
    }));
    const subClassCodeList = Array.from(subClassSet).map((subName) => ({
      text: subName,
    }));
    if (classCodeList.length !== 0) {
      this.setState({ ClassCodeListData: classCodeList });
    }
    if (subClassCodeList.length !== 0) {
      this.setState({
        Sub_class_codeListData: subClassCodeList,
      });
    } else {
      this.setState({ Sub_class_codeListData: [] });
    }
  };

  // downloadFile(event: any, option: any) {
  //   this.setState({ isDropdownOpen: false });
  //   const fromDate = this.state.fromDate
  //     ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
  //     : undefined;
  //   const toDate = this.state.toDate
  //     ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
  //     : undefined;
  //   const season = [...new Set(this.state.selectedSeasons)];
  //   const deptCode = [...new Set(this.state.selecteddepartment)];
  //   const classCode = [...new Set(this.state.selectedclass)];
  //   const subclassCode = [...new Set(this.state.selectedsubclass)];
  //   const token = this.token;

  //   let url = '';
  //   let fileName = '';

  //   if (option.text === 'All Attributes Insights') {
  //     url = `${baseUrl}/api/Product/AttributesExcel`;
  //     fileName = 'All Attributes Insights.xlsx';
  //   } else if (option.text === 'UID Attributes Insights') {
  //     url = `${baseUrl}/api/Product/ProductExportExcel`;
  //     fileName = 'UID Attributes Insights.xlsx';
  //   } else if (option.text === 'T1 Attributes Insights') {
  //     url = `${baseUrl}/api/Product/AllTemplateExcel`;
  //     fileName = 'T1 Attributes Insights.xlsx';
  //   } else if (option.text === 'T2 Attributes Insights') {
  //     url = `${baseUrl}/api/Product/AllTemplateExcel`;
  //     fileName = 'T2 Attributes Insights.xlsx';
  //   } else if (option.text === 'QC Attributes Insights') {
  //     url = `${baseUrl}/api/Product/AllTemplateExcel`;
  //     fileName = 'QC Attributes Insights.xlsx';
  //   } else if (option.text === 'Packaging Attributes Insights') {
  //     url = `${baseUrl}/api/Product/AllTemplateExcel`;
  //     fileName = 'Packaging Attributes Insights.xlsx';
  //   }

  //   url += `?${Object.entries({ token })
  //     .map(([key, value]) => `${key}=${"value"}`)
  //     .join('&')}`;
  //   if (fromDate) {
  //     url += `&fromDate=${fromDate}`;
  //   }
  //   if (toDate) {
  //     url += `&toDate=${toDate}`;
  //   }
  //   if (season.length !== 0) {
  //     url += `&season=${season}`;
  //   }
  //   if (deptCode.length !== 0) {
  //     url += `&dept_code=${deptCode}`;
  //   }
  //   if (classCode.length !== 0) {
  //     url += `&class_code=${classCode}`;
  //   }
  //   if (subclassCode.length !== 0) {
  //     url += `&subclass_code=${subclassCode}`;
  //   }
  //   if(option.text === 'T1 Attributes Insights'){
  //     url += "&templateType=4";
  //   }
  //   if(option.text === 'T2 Attributes Insights'){
  //     url += "&templateType=6,12";
  //   }
  //   if(option.text === 'QC Attributes Insights'){
  //     url += "&templateType=8";
  //   }
  //   if(option.text === 'Packaging Attributes Insights'){
  //     url += "&templateType=10";
  //   }
  //   if (this.roleId.length !== 0) {
  //     this.roleId && this.roleId.forEach(item => { url += `&roleIds=${item}` });
  //   }

  //   try {
  //     downloadFileUID(url, fileName);
  //   } catch (error: any) {
  //     this.notify.showErrorNotify(error);
  //   }
  // }
  downloadFile(event: any, option: any) {
    this.setState({ isDropdownOpen: false });
    const fromDate = this.state.fromDate
      ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
      : undefined;
    const toDate = this.state.toDate
      ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
      : undefined;
    const season = [...new Set(this.state.selectedSeasons)];
    const deptCode = [...new Set(this.state.selecteddepartment)];
    const classCode = [...new Set(this.state.selectedclass)];
    const subclassCode = [...new Set(this.state.selectedsubclass)];
    const token = this.token;

    let url = '';
    let fileName = '';

    
    switch (option.text) {
        case 'All Attributes Insights':
            url = `${baseUrl}/api/Product/AttributesExcel`;
            fileName = 'All Attributes Insights.xlsx';
            break;
        case 'UID Attributes Insights':
            url = `${baseUrl}/api/Product/ProductExportExcel`;
            fileName = 'UID Attributes Insights.xlsx';
            break;
        case 'T1 Attributes Insights':
            url = `${baseUrl}/api/Product/AllTemplateExcel`;
            fileName = 'T1 Attributes Insights.xlsx';
            break;
        case 'T2 Attributes Insights':
            url = `${baseUrl}/api/Product/AllTemplateExcel`;
            fileName = 'T2 Attributes Insights.xlsx';
            break;
        case 'QC Attributes Insights':
            url = `${baseUrl}/api/Product/AllTemplateExcel`;
            fileName = 'QC Attributes Insights.xlsx';
            break;
        case 'Packaging Attributes Insights':
            url = `${baseUrl}/api/Product/AllTemplateExcel`;
            fileName = 'Packaging Attributes Insights.xlsx';
            break;
        default:
            return;
    }

    const exportModel = new AttributesExportModel();
    exportModel.fromDate = fromDate;
    exportModel.toDate = toDate;
    exportModel.season = season.length ? season.join(',') : undefined;
    exportModel.deptCode = deptCode.length ? deptCode.join(',') : undefined;
    exportModel.classCode = classCode.length ? classCode.join(',') : undefined;
    exportModel.subClassCode = subclassCode.length ? subclassCode.join(',') : undefined;
    exportModel.roleIds = this.roleId.length ? this.roleId : undefined;

    
    switch (option.text) {
        case 'T1 Attributes Insights':
            exportModel.templatetype = '4';
            break;
        case 'T2 Attributes Insights':
            exportModel.templatetype = '6,12';
            break;
        case 'QC Attributes Insights':
            exportModel.templatetype = '8';
            break;
        case 'Packaging Attributes Insights':
            exportModel.templatetype = '10';
            break;
    }

    try {
        downloadFileUID(url, fileName, exportModel);
    } catch (error: any) {
        this.notify.showErrorNotify(error);
    }
}

  downloadImage(ImageUrl: any) {
    this.setState({ imgVisible: false });
    try {
      const token = this.token;
      let url = `${baseUrl}/api/Product/DownloadImage`;
      url += `?${Object.entries({ token })
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      if (ImageUrl) {
        url += `&imageUrl=${ImageUrl}`;
      }
      downloadImageUID(url, ImageUrl);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  DeleteImage(imageId:any){
    if(imageId != null || imageId != undefined){
    this.apiClient.deleteproductimage(imageId).then((response: any) => {

      if (response.hasError) {
        this.notify.showErrorNotify(response.message);
      } else {
        let message = `${this.props.t('Image Deleted Successfully')}`;
        this.notify.showErrorNotify(message);
        this.setState({ imgVisible: false ,confirmVisible: false })
        this.refresh(null,null);
      }
    });
  }else{
    this.notify.showErrorNotify('Image Id should not be null or undefined');
  }
}
  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue, searchKey: newValue });

    var result = this.state.searchData.filter((row: any) => {
      const uid = row?.uid?.toLowerCase();
      const planogramName = row?.planogram_familycode;
      if (typeof uid === 'string' && typeof planogramName === 'string') {
        return (
          uid.includes(newValue.trim().split(/ +/).join(' ').toLowerCase()) ||
          planogramName.toLowerCase().includes(newValue.trim().split(/ +/).join(' ').toLowerCase())
        );
      } else if (typeof uid === 'string') {
        return uid.includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
      } else if (typeof planogramName === 'string') {
        return planogramName
          .toLowerCase()
          .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
      }
      return false;
    });
    const uidData = result?.map((item: any, index: any) => ({ index: index + 1, ...item }));
    this.setState({
      uidData: uidData,
      exportvisible: uidData.length !== 0 ? true : false,
      searchval: newValue,
    });
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
    const dropdownElement = document.querySelector('.your-dropdown-container');
    if (dropdownElement && !dropdownElement.contains(event.target)) {
      this.closeDropdown();
    }
  };
  handleMouseEnter = () => {
    this.setState({ isMouseOverDropdown: true });
  };

  handleMouseLeave = () => {
    this.setState({ isMouseOverDropdown: false });
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

  columnFilter = async () => {
    var Filter1: any = [];
    var Filter2: any = [];
    var Filter3: any = [];
    var Filter4: any = [];
    var date: any = [];
    const { searchData, selectedSeasons, selectedsubclass, selectedclass, selecteddepartment } =
      this.state;
    if (this.state.fromDate !== null || this.state.toDate !== null) {
      if (
        this.state.fromDate !== null &&
        this.state.fromDate !== undefined &&
        this.state.toDate !== null &&
        this.state.toDate !== undefined
      ) {
        date = searchData.filter((row: any) => {
          let formattedfromdate;
          let formattedtodate;

          if (this.state.fromDate !== null) {
            const fromdate = new Date(this.state.fromDate);
            formattedfromdate = new Date(
              fromdate.getFullYear(),
              fromdate.getMonth(),
              fromdate.getDate(),
            );
            formattedfromdate = `${
              formattedfromdate.getMonth() + 1
            }/${formattedfromdate.getDate()}/${formattedfromdate.getFullYear()}`;
          }

          if (this.state.toDate !== null) {
            const todate = new Date(this.state.toDate);
            formattedtodate = new Date(todate.getFullYear(), todate.getMonth(), todate.getDate());
            formattedtodate = `${
              formattedtodate.getMonth() + 1
            }/${formattedtodate.getDate()}/${formattedtodate.getFullYear()}`;
          }
          const rowDateCreated = new Date(row.datecreated);
          if (
            formattedfromdate &&
            formattedtodate &&
            !isNaN(rowDateCreated.getTime()) &&
            rowDateCreated >= new Date(formattedfromdate) &&
            rowDateCreated <= new Date(formattedtodate)
          ) {
            return true;
          } else {
            return false;
          }
        });
      } else {
        date = searchData.filter((row: any) => {
          let formattedfromdate;
          let formattedtodate;

          if (this.state.fromDate !== null) {
            const fromdate = new Date(this.state.fromDate);
            formattedfromdate = new Date(
              fromdate.getFullYear(),
              fromdate.getMonth(),
              fromdate.getDate(),
            );
            formattedfromdate = `${
              formattedfromdate.getMonth() + 1
            }/${formattedfromdate.getDate()}/${formattedfromdate.getFullYear()}`;
          }

          if (this.state.toDate !== null) {
            const todate = new Date(this.state.toDate);
            formattedtodate = new Date(todate.getFullYear(), todate.getMonth(), todate.getDate());
            formattedtodate = `${
              formattedtodate.getMonth() + 1
            }/${formattedtodate.getDate()}/${formattedtodate.getFullYear()}`;
          }
          const rowDateCreated = new Date(row.datecreated);
          if (
            (formattedfromdate &&
              !isNaN(rowDateCreated.getTime()) &&
              rowDateCreated >= new Date(formattedfromdate)) ||
            (formattedtodate &&
              !isNaN(rowDateCreated.getTime()) &&
              rowDateCreated <= new Date(formattedtodate))
          ) {
            return true;
          } else {
            return false;
          }
        });
      }
    } else {
      date = searchData;
    }

    if (selectedSeasons.length !== 0) {
      Filter1 = date.filter((row: any) => {
        return selectedSeasons.includes(row.seasoncode);
      });
    } else {
      Filter1 = date;
    }
    if (selecteddepartment.length != 0) {
      Filter2 = Filter1.filter((row: any) => {
        return selecteddepartment.includes(row.deptcode);
      });
    } else {
      Filter2 = Filter1;
    }
    if (selectedclass.length != 0) {
      Filter3 = Filter2.filter((row: any) => {
        return selectedclass.includes(row.classcode);
      });
    } else {
      Filter3 = Filter2;
    }
    if (selectedsubclass.length != 0) {
      Filter4 = Filter3.filter((row: any) => {
        return selectedsubclass.includes(row.subclasscode);
      });
    } else {
      Filter4 = Filter3;
    }
    this.setState({
      uidData: Filter4,
      exportvisible: Filter4.length !== 0 ? true : false,
    });
    if (this.dropdownOptions.length === 0) {
      const newItem: IDropdownOption = {
        key: 'exportAll',
        text: this.props.t('Export All PIM Attributes'),
      };
      this.dropdownOptions.push(newItem);
    }
    if (this.dropdownOptions.length === 1) {
      const newItem: IDropdownOption = {
        key: 'exportScreen',
        text: this.props.t('Export Current View'),
      };
      this.dropdownOptions.push(newItem);
    }
    if (this.dropdownOptions.length === 2) {
      const newItem: IDropdownOption = {
        key: 'exportPDT',
        text: this.props.t('Export Packaging Design Template'),
      };
      this.dropdownOptions.push(newItem);
    }
    if (this.dropdownOptions.length === 3 && Filter4.length === 0) {
      // this.dropdownOptions = this.dropdownOptions.slice(0, 1);
      if (this.dropdownOptions.length > 0) {
        this.dropdownOptions = [];
      }
    }
  };

  handleActiveSwitch = (uid:any, isActive: boolean) => {
    if(uid != null || uid != undefined){
      this.apiClient.updateUidIsactive(uid, isActive).then((response: any) => {
        if (response.hasError) {
          this.notify.showErrorNotify(response.message);
        } else {
          this.setState((prevState: any) => {
            const updatedUidData = prevState.uidData.map((item:any) =>
              item.uid === uid ? { ...item, isactive: isActive } : item
            );
            return { uidData: updatedUidData };
          }, () => {
            let message = isActive ? `${this.props.t('Uid Activated Successfully')}` : `${this.props.t('Uid Deactivated Successfully')}`;
            this.notify.showSuccessNotify(message);
          });
          this.setState({
            isUidActiveModalOpen: false,
            uidInModal: [],
            isActiveInModal: false
          })
          // this.refresh(null,null);
        }
      });
    }else{
      this.notify.showErrorNotify('Id should not be null or undefined');
    }
  }

  render() {
    const { t } = this.props;
    const { SeasonListData, departmentListData, ClassCodeListData, Sub_class_codeListData } =
      this.state;
    const allSeasonsSelected =
      SeasonListData && SeasonListData.length
        ? this.state.selectedSeasons.length === SeasonListData.length
        : false;
    const allDeptSelected =
      departmentListData && departmentListData.length
        ? this.state.selecteddepartment.length === departmentListData.length
        : false;
    const allClassSelected =
      ClassCodeListData && ClassCodeListData?.length
        ? this.state.selectedclass.length === ClassCodeListData.length
        : false;
    const allSubClassSelected =
      Sub_class_codeListData && Sub_class_codeListData?.length
        ? this.state.selectedsubclass.length === Sub_class_codeListData.length
        : false;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
      },
      {
        key: 'uid',
        name: <div className={columnHeader}>{`${this.props.t('COL_UID')}`}</div>,
        selector: (row: any) => <span title={row.uid}>{row.uid}</span>,
        isVisible: true,
        width: '170px',
      },
      {
        key: 'planogram_name',
        name: <div className={columnHeader}>{`${this.props.t('COL_PLANOGRAM_NAME')}`}</div>,
        selector: (row: any) => (
          <span title={row.planogram_familycode}>{row.planogram_familycode}</span>
        ),
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'col_divCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_DIV_NAME')}`}</div>,
        selector: (row: any) => <span title={row.divisioncode}>{row.divisioncode}</span>,
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'dept_code',
        name: <div className={columnHeader}>{`${this.props.t('COL_DEPARTMENT_NAME')}`}</div>,
        selector: (row: any) => {
          return <span title={row?.deptcode}>{row?.deptcode}</span>;
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'season',
        name: <div className={columnHeader}>{`${this.props.t('COL_SEASON')}`}</div>,
        selector: (row: any) => {
          const seasonCode = row.seasoncode || '';
          return typeof seasonCode === 'string' ? (
          <span title={seasonCode}>{seasonCode}</span>
          ) : seasonCode;
        },
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'product_desc',
        name: <div className={columnHeader}>{`${this.props.t('COL_PRODUCT_DES')}`}</div>,
        selector: (row: any) => {
          // const productDesc = row.columnData && row.columnData['Product Desc'];
          return <span title={row.product_desc}>{row.product_desc}</span>;
        },
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'Group_code',
        name: <div className={columnHeader}>{`${this.props.t('COL_GROUP_NAME')}`}</div>,
        selector: (row: any) => <span title={row.groupcode}>{row.groupcode}</span>,
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'classCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_CLASS_NAME')}`}</div>,
        selector: (row: any) => <span title={row.classcode}>{row.classcode}</span>,
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'Sub_classCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_SUB_CLASS_NAME')}`}</div>,
        selector: (row: any) => <span title={row.subclasscode}>{row.subclasscode}</span>,
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'Target MRP',
        name: <div className={columnHeader}>{`${this.props.t('Target MRP')}`}</div>,
        selector: (row: any) => {
          // const TargetMRP = row.columnData && row.columnData['Target MRP'];
          return <span title={row.target_mrp}>{row.target_mrp}</span>;
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'Product Purpose',
        name: <div className={columnHeader}>{`${this.props.t('Product Purpose')}`}</div>,
        selector: (row: any) => {
          // const ProductPurpose = row.columnData && row.PurposeValue;
          return <span title={row.productpurpose}>{row.productpurpose}</span>;
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'Price Grouping',
        name: <div className={columnHeader}>{`${this.props.t('Price Grouping')}`}</div>,
        selector: (row: any) => {
          // const PriceGrouping = row.columnData && row.columnData['Price Grouping'];
          return <span title={row.pricegrouping}>{row.pricegrouping}</span>;
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'productImage',
        name: <div className={columnHeader}>{`${this.props.t('COL_PICTURE')}`}</div>,
        selector: (row: any) => {
          const url = this.state.pictureData?.find((e: any) => e.id == row.pictureid)?.imageUrl;
          const ImageName = this.state.pictureData?.find((e: any) => e.id == row.pictureid)?.name;
          const value = url?.split('?');
          const ImgUrl = value == undefined || null ? '' : value[0];
          return (
            <>
              {ImgUrl && (
                <img
                  src={ImgUrl}
                  alt={this.props.t('VIEW_IMAGE')}
                  style={{ width: '30.5px', height: '30.5px' }}
                  onClick={() => {
                    this.setState({
                      imgVisible: true,
                      imageURL: ImgUrl,
                      imageId:row.pictureid,
                      imageLoading: ImgUrl === '' ? true : false,
                      imageDownload: ImageName,
                    });
                  }}
                />
              )}
            </>
          );
        },
        isVisible: true,
        minWidth: '60px',
      },
      {
        key: 'Action',
        name: <div className={columnHeader}>{`${this.props.t('COL_ACTION')}`}</div>,
        selector: (row: any) => {
          if(this.state.uidData.length > 0){
          const handleActiveSwitch = (uid:any, isActive: boolean) => {
            this.setState({
              isUidActiveModalOpen : true,
              uidInModal: uid,
              isActiveInModal: isActive
            })
          }
          return <span title={row.isactive ? "Active" : "Inactive"}>
            <Switch
              className ={row.isactive ? "switchButtonOn" : "switchButtonOff" }
              checked={row.isactive}
              onChange={(e:any)=>handleActiveSwitch(row.uid, !row.isactive)}
            />
            </span>;
          }
        },
        isVisible: this.roleId.includes(1) ? true : false,
        minWidth: '130px',
      },
    ].filter((e: any) => e.isVisible == true);

    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    const { isDropdownOpen, hoveredOption } = this.state;
    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Stack>
            {/* <Text block className="text">{`${t('HEADER_PIM')} / ${t('UID')}`}</Text> */}
            <Grid container spacing={1} direction="row" alignItems="center" className="grid">
              <Grid item lg={10.6} xs={8}>
                <Text
                  variant="xxLarge"
                  styles={PIMHearderText}
                  className="color-blue text"
                >{`${this.props.t('UID_RECORDS')}`}</Text>
              </Grid>
              <Grid item lg={0.9} xs={2} />
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyle"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={this.toggleDropdown}
                />
                {isDropdownOpen && (
                  <div
                    className={mergeStyles('your-dropdown-container', {
                      position: 'absolute',
                      top: '30px',
                      right: '45px',
                      zIndex: 9999,
                      backgroundColor: 'rgb(210, 238, 234)',
                      borderRadius: '5px',
                    })}
                    onMouseEnter={this.handleMouseEnter}
                    onMouseLeave={this.handleMouseLeave}
                  >
                    <ul style={{ listStyleType: 'none', padding: '5px 0px', margin: 0 }}>
                      {this.dropdownOptions.map((option) => (
                        <li
                          key={option.key}
                          onClick={() => {
                            this.downloadFile(null, option);
                            this.setState({ hoveredOption: null });
                          }}
                          onMouseEnter={() => this.handleOptionMouseEnter(option)}
                          onMouseLeave={this.handleOptionMouseLeave}
                          style={{
                            fontFamily: 'Montserrat, sans-serif',
                            padding: '1px 16px',
                            lineHeight: '1.5',
                            fontSize: '14px',
                            cursor: 'pointer',
                            fontWeight: 600,
                            backgroundColor: option === hoveredOption ? 'lightgray' : 'transparent',
                          }}
                        >
                          {option.text}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </Grid>
            </Grid>
          </Stack>
          <Grid lg={12} item container spacing={0.8} direction={'row'}>
            <Grid item lg={1.92} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={`${t('UID_PLANOGRAM_SEARCH_PLACEHOLDER')}`}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={1.4} xs={6}>
              <ComboBox
                placeholder={`${this.props.t('COL_SEASON')}`}
                multiSelect
                styles={PIMDropdownStyles}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allSeasonsSelected },
                  ...(this.state.SeasonListData || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.selectedSeasons)
                      ? this.state.selectedSeasons.includes(option.text)
                      : false,
                  })),
                ]}
                key={this.state.refreshKey}
                onChange={(e: any, selectedOption: any) => {
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected =
                      (this.state.selectedSeasons || []).length ===
                      (this.state.SeasonListData || []).length;
                    const newSelectedSeasons = allSelected
                      ? []
                      : (this.state.SeasonListData || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);
                    this.setState({ selectedSeasons: newSelectedSeasons }, () => {
                      this.columnFilter();
                    });
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.selectedSeasons)
                      ? [...this.state.selectedSeasons]
                      : [];
                    const isSelected = Array.isArray(this.state.selectedSeasons)
                      ? this.state.selectedSeasons.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                    }
                    this.setState({ selectedSeasons: updatedSelectedSeasons }, () => {
                      this.columnFilter();
                    });
                  }
                }}
                selectedKey={this.state.selectedSeasons || []}
              />
            </Grid>
            <Grid item lg={1.7} xs={6}>
              <ComboBox
                multiSelect
                placeholder={`${t('UID_DEPT_DROPDOWN')}`}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allDeptSelected },
                  ...(this.state.departmentListData || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.selecteddepartment)
                      ? this.state.selecteddepartment.includes(option.text)
                      : false,
                  })),
                ]}
                styles={PIMDropdownStyles1}
                key={this.state.refreshKey}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    ClassCodeListData: [],
                    Sub_class_codeListData: [],
                    refreshKey1: this.state.refreshKey1 + 1,
                    refreshKey2: this.state.refreshKey2 + 1,
                    selectedsubclass: [],
                    selectedclass: [],
                  });
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected1 =
                      (this.state.selecteddepartment || []).length ===
                      (this.state.departmentListData || []).length;
                    const newSelectedSeasons = allSelected1
                      ? []
                      : (this.state.departmentListData || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);

                    this.setState({ selecteddepartment: newSelectedSeasons }, () => {
                      // this.columnFilter1();
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey1: this.state.refreshKey1 + 1,
                        refreshKey2: this.state.refreshKey2 + 1,
                        selectedsubclass: [],
                        selectedclass: [],
                      });
                      this.columnFilter();
                      this.DropdownFilter();
                    });
                  } else {
                    this.setState({
                      ClassCodeListData: [],
                      Sub_class_codeListData: [],
                      refreshKey1: this.state.refreshKey1 + 1,
                      refreshKey2: this.state.refreshKey2 + 1,
                      selectedsubclass: [],
                      selectedclass: [],
                    });
                    const updatedSelectedDept = Array.isArray(this.state.selecteddepartment)
                      ? [...this.state.selecteddepartment]
                      : [];
                    const isSelected1 = Array.isArray(this.state.selecteddepartment)
                      ? this.state.selecteddepartment.includes(selectedOption.text)
                      : false;
                    if (isSelected1) {
                      const index = updatedSelectedDept.indexOf(selectedOption.text);
                      updatedSelectedDept.splice(index, 1);
                    } else {
                      updatedSelectedDept.push(selectedOption.text);
                    }
                    this.setState({ selecteddepartment: updatedSelectedDept }, () => {
                      // this.columnFilter1();
                      this.selectedClassCode1 = [];
                      this.selectedSubClassCode1 = [];
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey2: this.state.refreshKey2 + 1,
                      });
                      this.columnFilter();
                      this.DropdownFilter();
                    });
                  }
                }}
                selectedKey={this.state.selecteddepartment || []}
              />
            </Grid>
            <Grid item lg={1.7} xs={6}>
              <ComboBox
                multiSelect
                placeholder={`${t('UID_CLASSNAME_DROPDOWN')}`}
                disabled={this.state.selecteddepartment.length == 0}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allClassSelected },
                  ...(this.state.ClassCodeListData || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.selectedclass)
                      ? this.state.selectedclass.includes(option.text)
                      : false,
                  })),
                ]}
                styles={PIMDropdownStyles1}
                key={this.state.refreshKey1}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected1 =
                      (this.state.selectedclass || []).length ===
                      (this.state.ClassCodeListData || []).length;
                    const newSelectedSeasons = allSelected1
                      ? []
                      : (this.state.ClassCodeListData || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);

                    this.setState({ selectedclass: newSelectedSeasons }, () => {
                      this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                      this.columnFilter();
                      this.DropdownFilter();
                    });
                  } else {
                    const updatedSelectedDept = Array.isArray(this.state.selectedclass)
                      ? [...this.state.selectedclass]
                      : [];
                    const isSelected1 = Array.isArray(this.state.selectedclass)
                      ? this.state.selectedclass.includes(selectedOption.text)
                      : false;
                    if (isSelected1) {
                      const index = updatedSelectedDept.indexOf(selectedOption.text);
                      updatedSelectedDept.splice(index, 1);
                    } else {
                      updatedSelectedDept.push(selectedOption.text);
                    }
                    this.setState({ selectedclass: updatedSelectedDept }, () => {
                      this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                      this.columnFilter();
                      this.DropdownFilter();
                    });
                  }
                }}
                selectedKey={this.state.selectedclass || []}
              />
            </Grid>
            <Grid item lg={1.6} xs={6}>
              <ComboBox
                multiSelect
                placeholder={`${t('UID_SUBCLASSNAME_DROPDOWN')}`}
                disabled={this.state.selectedclass.length == 0}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allSubClassSelected },
                  ...(this.state.Sub_class_codeListData || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.selectedsubclass)
                      ? this.state.selectedsubclass.includes(option.text)
                      : false,
                  })),
                ]}
                styles={PIMDropdownStyles1}
                onChange={(e: any, selectedOption: any) => {
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected1 =
                      (this.state.selectedsubclass || []).length ===
                      (this.state.Sub_class_codeListData || []).length;
                    const newSelectedSeasons = allSelected1
                      ? []
                      : (this.state.Sub_class_codeListData || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);

                    this.setState({ selectedsubclass: newSelectedSeasons }, () => {
                      this.columnFilter();
                    });
                  } else {
                    const updatedSelectedDept = Array.isArray(this.state.selectedsubclass)
                      ? [...this.state.selectedsubclass]
                      : [];
                    const isSelected1 = Array.isArray(this.state.selectedsubclass)
                      ? this.state.selectedsubclass.includes(selectedOption.text)
                      : false;
                    if (isSelected1) {
                      const index = updatedSelectedDept.indexOf(selectedOption.text);
                      updatedSelectedDept.splice(index, 1);
                    } else {
                      updatedSelectedDept.push(selectedOption.text);
                    }
                    this.setState({ selectedsubclass: updatedSelectedDept }, () => {
                      this.columnFilter();
                    });
                  }
                }}
                key={this.state.refreshKey2}
                selectedKey={this.state.selectedsubclass || []}
              />
            </Grid>
            <Grid item lg={2} xs={6}>
              <CommonDate
                onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                onToDateChange={(e: any) => this.handleToDateChange(e)}
                label={`${t('UIDCREATION_DATE')}`}
                ref={this.commonDateRef}
              />
            </Grid>
            <Grid item lg={0.18} xs={0} />
            <Grid item lg={0.5} xs={1.8}>
              <Icon
                iconName="Search"
                title={t('BTN_SEARCH')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.fromDate || this.state.toDate) {
                    // this.refresh(this.state.fromDate, this.state.toDate);
                    this.columnFilter();
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={1.8}>
              <Icon
                iconName="Clear"
                title={t('BTN_CLEAR')}
                className="iconStyle"
                onClick={() => {
                  if (
                    this.state.fromDate ||
                    this.state.toDate ||
                    this.state.selecteddepartment.length != 0 ||
                    this.state.selectedSeasons.length !== 0 ||
                    // this.selectedClassCode1.length !== 0 ||
                    // this.selectedDeptno1.length !== 0 ||
                    // this.selectedSeason1.length !== 0 ||
                    this.state.searchKey != ''
                      ? true
                      : false
                  ) {
                    this.setState({ fromDate: null, toDate: null });
                    this.refresh(null, null);
                    this.ClearDates();
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={1.8}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={async () => {
                  this.setState({ isRecord: true });
                  await this.refresh(null, null);
                  await this.refresh(this.state.fromDate, this.state.toDate);
                  this.setState({ isRecord: false });
                  this.ClearDates();
                }}
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.imgVisible}
            containerClassName={PIMcontentStyles.imageContainer}
            // isBlocking={true}
            onDismiss={() =>
              this.setState({
                imgVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <span className="apptextUID">{t('IMAGE_UPLOAD')}</span>
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => this.setState({ imgVisible: false })}
              />
              <Grid lg={12} item container spacing={0.8} direction={'row'}>
                {this.roleId.includes(1) ?
              (<Grid item lg={10.6} xs={10.6}>
              <IconButton
                className='deleteicon'
                iconProps={DeleteIcon}
                ariaLabel="Delete the Image"
                onClick={() => this.setState({ confirmVisible: true })}
              />
              </Grid>) : <Grid item lg={10.6} xs={10.6} />
              }
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyledownload"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={() => {
                    this.downloadImage(this.state.imageDownload);
                  }}
                />
              </Grid>
              </Grid>
            </div>
            <div className={PIMcontentStyles.imagebody}>
              {this.state.imageLoading == false ? (
                <Image
                  src={this.state.imageURL}
                  alt="picture"
                  style={{ maxWidth: '500px', maxHeight: '400px', width: '100%', height: '100%' }}
                />
              ) : (
                <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
              )}
            </div>
          </Modal>
          <Modal
          isOpen={this.state.confirmVisible}
          containerClassName={PIMcontentStyles.confirmContainer}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              confirmVisible: false,
              row: [],
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ confirmVisible: false, row: [] })}
          />
          <div className={PIMcontentStyles.confirmbody}>
            <Text variant="xLarge" className="apptext">{`${this.props.t(
              'DELETE_CONFIRM_MESSAGE',
            )}`}</Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => this.DeleteImage(this.state.imageId)}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ confirmVisible: false, row: [] });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.isUidActiveModalOpen}
          containerClassName={PIMcontentStyles.confirmContainer}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              isUidActiveModalOpen: false,
              uidInModal: [],
              isActiveInModal: false
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({
              isUidActiveModalOpen: false,
              uidInModal: [],
              isActiveInModal: false })}
          />
          <div className={PIMcontentStyles.confirmbody}>
          {this.state.isActiveInModal ?
            (
              <Text variant="xLarge" className="apptext2">{`${t('CONFIRM_ACTIVATE')}`}</Text>
            ) : (
              <Text variant="xLarge" className="apptext2">{`${t('CONFIRM_DEACTIVATE')}`}</Text>
            )}{' '}
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => this.handleActiveSwitch(this.state.uidInModal,this.state.isActiveInModal)}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({
                      isUidActiveModalOpen: false,
                      uidInModal: [],
                      isActiveInModal: false
                     });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <div className="dataTableFixedFooter">
          <DataTable
            className="CAMdatatable"
            columns={columns}
            data={this.state.uidData}
            onColumnOrderChange={handleColumnReorder}
            pagination={true}
            selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeader
            fixedHeaderScrollHeight="68.5vh"
            striped
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {<DataTable columns={columns} data={[{ '': '' }]} />}

                <Stack className="noRecordsWidth">
                  {this.state.isRecord == true
                    ? `${this.props.t('RECORDS')}`
                    : `${this.props.t('NO_RECORDS')}`}
                </Stack>
              </div>
            }
          />
           </div>
        </div>
      </div>
    );
  }
}
const ComponentTranslated = withTranslation()(UID);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
