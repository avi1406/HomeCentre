import {
  DefaultButton,
  Dropdown,
  IComboBoxOption,
  Icon,
  IconButton,
  Modal,
  SearchBox,
  Stack,
  Image,
  TextField,
  ComboBox,
  IDropdownOption,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import ApiManager from '@/services/ApiManager';
import {
  cancelIcon,
  ChevronLeft,
  ChevronRight,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  iconButtonStyles1,
  iconButtonStyles12,
  iconButtonStyles123,
  rejectComment,
} from '@/common/properties';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import {
  PIMHearderText,
  PIMButtons,
  PIMdropdownStyles2,
  PIMHeaderButtons,
  columnHeader,
  hyperlink,
  PIMsearchBoxStyles,
  PIMcontentStyles,
  PIMdropdownStyles21,
  PIMdropdownStylesDd,
  PIMcontentStylesQC,
  PIMHeaderButtonsQc,
  PIMButtonsQc,
  PIMfooterButtonConfirm,
} from '../PimStyles';
import { baseUrl } from '@/services/Constants';
import DataTable from 'react-data-table-component';
import Util from '@/services/Util';
import axios from 'axios';
import CommonDate, { CommonDateRef } from '../../pages/commonFunctions';
import { FaRegClone } from 'react-icons/fa';
import { ProductImageModel, ReceivedProductsModel } from '@/services/ApiClient';
import PhotoShootTracking from './PhotoShootTracking';
import AddOrUpdateReceivedProducts from '../ReceivedProduct/AddOrUpateReceivedproduct';

class CRTPhotoShootTracking extends React.Component<any, any> {
  location = window.location.pathname;
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  utils = new Util();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  roleId: any[] = this.user?.roleId || [];
  deptId: any[] = this.user?.departmentId || [];
  username: any = this.user?.name || '';
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  rolelvl: any = this.decode.RoleLevel;
  fieldType: IComboBoxOption[] | undefined;
  selectedRowData: null | undefined;
  rejected = false;
  commonDateRefs: React.RefObject<CommonDateRef>[] = Array.from({ length: 2 }, () =>
    React.createRef<CommonDateRef>(),
  );
  statuss: any;
  PTstatus: any = [];
  TypeColText: any = [];
  constructor(props: any) {
    super(props);

    this.state = {
      currentRecord: [],
      searchKey: '',
      selectedRowData: [],
      noSelection: false,
      row: [],
      dropDownSelectedRecord: [],
      searchResult: [],
      filteredRecord: [],
      selecteddept: [],
      showrecivedprd: false,
      Showrejecteditem: false,
      isRecord: true,
      isRecord1: true,
      exportvisible: false,
      statuss: [],
      rejecteddata: [],
      showActionColumn: true,
      A_D_Visible: false,
      imgVisible: false,
      imageURL: [],
      divisionSelected: '',
      pictureData: [],
      currentRecord1: [],
      recjected: false,
      handoverOptions1: [],
      dropdown: '',
      DropdownVal: 'All',
      refreshKey: 0,
      fullImageVisible: false,
      fullImageSrc: '',
      statusdropdown: '',
    };
  }

  async componentDidMount(): Promise<void> {
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const handoverStatus = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'PhotoshootStatus')?.jsonValue,
    );
    var handoverOption: { key: number | undefined; text: string | undefined }[] = [];
    handoverStatus?.forEach((element: any) => {
      handoverOption.push({ key: element.key, text: element.text });
    });
    this.refresh();
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    var departmentList = (await this.apiClient.getDivision())?.result;

    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    // deptOpt.push({ key: 1, text: 'All' }); this.decode.DepartmentId
    // departmentList?.forEach((element) => {
    //   departmentOption.push({ key: element.id, text: element.name });
    //   deptOpt.push({ key: element.id, text: element.name });
    // });

    // filteredDeptId.forEach((id) => {
    //   const matchingDepartment = departmentList?.find((dept) => dept.id === id);
    //   if (matchingDepartment) {
    //     departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //     deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //   }
    // });
    departmentList?.forEach((dept) => {
      const matchingId = filteredDeptId?.find((id) => id === dept.id);
      const matchingDepartment = departmentList?.find((dept) => dept.id === matchingId);
      if (matchingDepartment) {
        departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
        deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
      }
    });
    var ProductImageType = (await this.apiClient.getProductImageType())?.result;
    var ProductImageoption: { key: number | undefined; text: string | undefined }[] = [];
    ProductImageType?.forEach((element) => {
      if (element.name !== 'All') {
        ProductImageoption.push({ key: element.id, text: element.name });
      }
    });
    this.setState({
      handoverOptions1: handoverOption,
      departmentOption: departmentOption,
      ProductImageoptions: ProductImageoption,
      deptOption: deptOpt,
      filteredDeptId: filteredDeptId,
      deptfilteredDeptId: filteredDeptId,
    });
  }
  async refresh() {
    this.PTstatus = [];
    this.TypeColText = [];
    this.setState({
      refreshKey: this.state.refreshKey + 1,
      selecteddept: [],
      rejecteddata: [],
      isRecord: true,
      isRecord1: true,
    });
    try {
      const picData = (await this.apiClient.getPSFileNameAndUrl()).result;
      this.setState({ searchKey: '' });
      let response: any = [];
      if (this.rejected == false) {
        response = (await this.apiClient.getReceivedProductsRecords()).result;
        this.setState({
          currentRecord1: [],
          dropDownSelectedRecord: [],
          rejecteddata: [],
        });
      } else {
        response = (await this.apiClient.getRejectedItemcode()).result;
        this.setState({
          currentRecord: [],
          dropDownSelectedRecord: [],
          //filteredRecord: [],
        });
      }
      const filteredResponse = response?.filter((item: any) =>
        this.deptId.includes(item.divisionId),
      );
      const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      const responeWithIndex1 = filteredResponse?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      var Photoshootstatus = (await this.apiClient.photoshootstatus())?.result;
      var PhotoSstatus: { key: number | undefined; text: string | undefined }[] = [];
      Photoshootstatus?.forEach((element) => {
        PhotoSstatus.push({ key: element.id, text: element.name });
      });
      this.setState({
        filteredRecord: [],
      });
      if (this.rejected == false) {
        this.setState({
          pictureData: picData,
          divisionSelected: '',
          PSstatus: PhotoSstatus,
          // pictureData: response,
          currentRecord: responeWithIndex,
          dropDownSelectedRecord: responeWithIndex,
          filteredRecord: responeWithIndex,
          isRecord: false,
          exportvisible: responeWithIndex.length !== 0 ? true : false,
          // DisplayRecordState:
          //   response.length === 0 ? (
          //     <h5 style={{ paddingTop: '2%' }}>{this.props.t('NO_RECORDS')}</h5>
          //   ) : (
          //     <h5 style={{ paddingTop: '10%' }}>{this.props.t('LOADING_RECORDS')}</h5>
          //   ),
        });
      } else {
        this.setState({
          divisionSelected: '',
          PSstatus: PhotoSstatus,
          // pictureData: response,
          rejecteddata: responeWithIndex1,
          currentRecord1: responeWithIndex1,
          dropDownSelectedRecord: responeWithIndex,
          isRecord1: false,
          exportvisible: responeWithIndex.length !== 0 ? true : false,
          // DisplayRecordState:
          //   response.length === 0 ? (
          //     <h5 style={{ paddingTop: '2%' }}>{this.props.t('NO_RECORDS')}</h5>
          //   ) : (
          //     <h5 style={{ paddingTop: '10%' }}>{this.props.t('LOADING_RECORDS')}</h5>
          //   ),
        });
      }
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const handoverStatus = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'PhotoshootStatus')?.jsonValue,
    );
    var handoverOption: { key: number | undefined; text: string | undefined }[] = [];
    handoverStatus?.forEach((element: any) => {
      handoverOption.push({ key: element.key, text: element.text });
    });
    this.setState({
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      minDate: undefined,
      minDate1: undefined,
      onClickToDate: undefined,
      onClickMinDate: undefined,
      toDate: undefined,
      toDate1: undefined,
      dropDownSelectedRecord: this.state.currentRecord,
      // filteredRecord: this.state.currentRecord,
      exportvisible: this.state.currentRecord.length !== 0 ? true : false,
      handoverOptions: handoverOption,
      PSstatusopt1: '',
      PSstatusopt: '',
      TypeColKey: '',
    });
    this.commonDateRefs.forEach((ref) => {
      if (ref.current) {
        ref.current.resetDates();
      }
    });
  }

  Imagedisplay = (itemCode: any) => {
    const imageURLs: string[] = [];
    let imageLoading = false;
    if (itemCode != null) {
      // itemCode.split(',').forEach((Id: any) => {
      const urlarray = this.state.currentRecord;
      let desiredObject;

      for (let i = 0; i < urlarray.length; i++) {
        if (urlarray[i].itemCode === itemCode) {
          desiredObject = urlarray[i];
          break;
        }
      }
      const ImgUrl = desiredObject.imageUrl;
      if (ImgUrl !== '') {
        imageURLs.push(ImgUrl);
      } else {
        imageLoading = true;
      }
      // });
      this.setState({
        imgVisible: true,
        imageURLs: imageURLs,
        imageLoading,
      });
    } else {
      this.setState({
        imgVisible: true,
        imageLoading,
      });
    }
  };
  closingImageModal = () => {
    this.setState({ imgVisible: false, imageURLs: [] });
  };

  columnFilter1 = async () => {
    var finalData: any = [];
    if (this.state.Showrejecteditem == false) {
      finalData = this.state.currentRecord;
    } else {
      finalData = this.state.currentRecord1;
    }

    if (this.PTstatus.length != 0) {
      if (this.PTstatus == 'Pending' || this.PTstatus == '1') {
        finalData = finalData.filter((element: any) => {
          return element.photoShootStatus == 'Pending';
        });
      } else if (this.PTstatus === 'Closed') {
        finalData = finalData.filter((element: any) => {
          return (
            ((element.sampleStatus == '2' || element.sampleStatus == 'Approved') &&
              element.camqcl1action == '1' &&
              element.productreceived == '1') ||
            ((element.sampleStatus == '2' || element.sampleStatus == 'Approved') &&
              element.camqcl1action == '1') ||
            element.sampleStatus == '1'
          );
        });
      } else {
        finalData = finalData.filter((element: any) => {
          return element.photoShootStatus == this.PTstatus;
        });
      }
    }

    if (this.TypeColText.length !== 0) {
      finalData = finalData.filter((element: any) => {
        return element.imageType == this.TypeColText;
      });
    }

    if (this.TypeColText.length === 0 && this.PTstatus.length === 0) {
      if (this.state.Showrejecteditem == false) {
        finalData = this.state.currentRecord;
      } else {
        finalData = this.state.currentRecord1;
      }
    }

    if (this.state.Showrejecteditem == false) {
      this.setState({
        filteredRecord: finalData,
      });
    } else {
      this.setState({
        rejecteddata: finalData,
      });
    }
  };

  downloadFile = (
    receivedfromDate: any,
    receivedtoDate: any,
    handOverfromDate: any,
    handOvertoDate: any,
    pSstatus: any,
    division: any,
  ) => {
    var body = new ReceivedProductsModel();
    body.handOverFromDate = this.state.minDate1?.toLocaleDateString('en-CA');
    body.handOverToDate = this.state.toDate1?.toLocaleDateString('en-CA');
    body.receivedFromDate = this.state.minDate?.toLocaleDateString('en-CA');
    body.receivedToDate = this.state.toDate?.toLocaleDateString('en-CA');
    body.division = this.state.selecteddept.toString();
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.join(',');
    body.userDivision = commaSeparatedString;
    body.pSstatus = this.state.statusdropdown;
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/ProductImage/GetAllFitersPSExportExcel`,
      method: 'POST',
      responseType: 'blob',
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'SKURecordsExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };
  downloadFileRejected = (downloadFromDate: any, downloadToDate: any) => {
    var body = new ProductImageModel();
    body.fromDate = this.state.downloadFromDate?.toLocaleDateString('en-CA');
    body.toDate = this.state.downloadToDate?.toLocaleDateString('en-CA');
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.join(',');
    body.userDivision = commaSeparatedString;
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/ProductImage/RejectedItemcodeExportExcel`,
      method: 'POST',
      responseType: 'blob',
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'RejectedSKUExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };

  resetDates = () => {
    this.setState({
      minDate: undefined,
      toDate: undefined,
    });
  };
  onSearch(e: any) {
    if (this.state.Showrejecteditem == false) {
      var newValue = e?.target.value;
      newValue = newValue == undefined ? '' : newValue;
      var { currentRecord } = this.state;
      var searchResult = currentRecord.filter((element: any) => {
        return element?.itemCode.toString().includes(newValue.trim());
      });
      this.setState({ filteredRecord: searchResult, searchKey: newValue });
      this.setState({
        filteredRecord: searchResult.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
        exportvisible: searchResult.length !== 0 ? true : false,
      });
    } else {
      var newValue = e?.target.value;
      newValue = newValue == undefined ? '' : newValue;
      var { currentRecord1 } = this.state;
      var searchResult = currentRecord1.filter((element: any) => {
        return element?.itemCode.toString().includes(newValue.trim());
      });
      this.setState({ rejecteddata: searchResult, searchKey: newValue });
      this.setState({
        rejecteddata: searchResult.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
        exportvisiblerejected: searchResult.length !== 0 ? true : false,
      });
    }
  }
  async onApproveBatch() {
    let { selectedRowData } = this.state;
    var check = true;
    if (
      selectedRowData.some(
        (item: {
          imageType: string | null;
          imageUrl: string | null;
          receivedDate: string | null;
        }) => {
          return (
            item.imageType === null &&
            item.imageType === 'Drape Image' &&
            (item.imageUrl === null || item.imageUrl === '') &&
            item.receivedDate === null
          );
        },
      )
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_QCACTION_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some((item: { imageType: string | null; imageUrl: string | null }) => {
        return (
          item.imageType === null &&
          item.imageType === 'Drape Image' &&
          (item.imageUrl === null || item.imageUrl === '')
        );
      })
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_TYPE_URL_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some(
        (item: {
          imageType: string | null;
          imageUrl: string | null;
          receivedDate: string | null;
        }) => {
          return (
            item.imageType === 'Drape Image' &&
            (item.imageUrl === null || item.imageUrl === '') &&
            item.receivedDate === null
          );
        },
      )
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_URL_DATE_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some(
        (item: {
          imageType: string | null;
          imageUrl: string | null;
          receivedDate: string | null;
        }) => {
          return item.imageType === null && item.receivedDate === null;
        },
      )
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_TYPE_DATE_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some((item: { imageType: string | null }) => {
        return item.imageType === null;
      })
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_TYPE_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some((item: { imageType: string | null; imageUrl: string | null }) => {
        return item.imageType === 'Drape Image' && (item.imageUrl === null || item.imageUrl === '');
      })
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_URL_ERROR'));
      check = false;
      return;
    } else if (
      selectedRowData.some((item: { receivedDate: string | null }) => {
        return item.receivedDate === null;
      })
    ) {
      this.notify.showErrorNotify(this.props.t('MSG_DATE_ERROR'));
      check = false;
      return;
    }
    if (this.state.Showrejecteditem == false) {
      if (
        selectedRowData.some((item: any) => {
          return (
            item.rejectedcount != null && item.rejectedreason != null && item.rejectedby !== null
          );
        })
      ) {
        this.notify.showErrorNotify(this.props.t('MSG_REJECTED_ERROR'));
        check = false;
        return;
      }
    }
    if (check) {
      var itemcode: any[] = [];
      var pSstatus = 'true';
      selectedRowData.forEach((element: any) => {
        const recordid = element.itemCode;
        itemcode.push(recordid);
      });
      let response: any = [];
      let responsereject: any = [];
      if (this.rejected == false) {
        response = await this.apiClient.updateQCRecords(itemcode, pSstatus, this.username);
      } else {
        responsereject = await this.apiClient.updateRejectedRecords(itemcode, this.username);
      }
      // const responeWithIndex = response
      //   ? response.map((item: any, index: any) => ({
      //       index: index + 1,
      //       ...item,
      //     }))
      //   : [];
      if (response && response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.setState({
          ApprovalEnable: true,
          ApproveSelectedClicked: false,
          // QcRecord: responeWithIndex,
        });
        this.notify.showSuccessNotify(this.props.t('SUBMITTED_SUCCESSFULLY'));
      }
    }
    this.refresh();
  }
  DefaultperpageReject = () => {
    const recordCont = 5;
    return recordCont;
  };
  perpageReject = () => {
    const recordCont = [5];
    return recordCont;
  };
  handleDropdownChange = (event: any, option?: IDropdownOption, index?: number) => {
    const { handoverOptions } = this.state;
    if (option) {
      let selectedKeys = [...this.state.divisionSelected];

      if (option.key === 'select-toggle') {
        // Toggle "Select All"
        if (option.selected) {
          // Select all options
          selectedKeys = handoverOptions.map((opt: any) => opt.key as string);
        } else {
          // Deselect all options
          selectedKeys = [];
        }
      } else {
        // Toggle individual option
        if (option.selected) {
          selectedKeys.push(option.key as string);
        } else {
          selectedKeys = selectedKeys.filter(
            (key) => key !== option.key && key !== 'select-toggle',
          );
        }
      }

      // Check if all individual options are selected
      const allOptionsSelected =
        handoverOptions.length > 0 && selectedKeys.length === handoverOptions.length;

      if (allOptionsSelected) {
        // Ensure "Select All" is selected if all options are selected
        if (!selectedKeys.includes('select-toggle')) {
          selectedKeys = ['select-toggle', ...selectedKeys];
        }
      } else {
        // Remove "Select All" if not all options are selected
        selectedKeys = selectedKeys.filter((key) => key !== 'select-toggle');
      }

      this.setState(
        {
          divisionSelected: selectedKeys,
          isDivision: true,
          searchKey: '',
        },
        () => {
          const selectedOptions = handoverOptions.filter((opt: any) =>
            selectedKeys.includes(opt.key as string),
          );
          const selectedTexts = selectedOptions.map((opt: any) => opt.text);
          const statusdropdown = selectedTexts.join(', ');
          this.setState({ statusdropdown });
        },
      );
    }
  };

  async onFilterClick() {
    var { toDate, minDate, minDate1, toDate1, statusdropdown } = this.state;
    // this.setState({ rejecteddata: [] });
    var fromDate = '';
    var receivedtoDate = '';
    var receivedfromDate = '';
    var handOvertoDate = '';
    var handOverfromDate = '';
    var pSstatus = this.state.statusdropdown;
    var camPstatus = '';
    var camRstatus = '';

    if (minDate != undefined) {
      const dateParts = new Date(minDate).toLocaleDateString('en-CA').split('/');
      fromDate = dateParts[0];
      receivedfromDate = fromDate;
    }
    if (toDate != undefined) {
      const dateParts = new Date(toDate).toLocaleDateString('en-CA').split('/');
      toDate = dateParts[0];
      receivedtoDate = toDate;
    }
    if (minDate1 != undefined) {
      const dateParts = new Date(minDate1).toLocaleDateString('en-CA').split('/');
      fromDate = dateParts[0];
      handOverfromDate = fromDate;
    }
    if (toDate1 != undefined) {
      const dateParts = new Date(toDate1).toLocaleDateString('en-CA').split('/');
      toDate = dateParts[0];
      handOvertoDate = toDate;
    }
    if (pSstatus != undefined) pSstatus = statusdropdown;
    let responsereject;
    let response;
    let resultString;
    resultString = this.state.selecteddept.map((divDeptText: string) => {
      const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
      return division ? division.key : null;
    });
    this.setState({ isRecord: true, isRecord1: true });
    if (this.rejected == true) {
      responsereject = (
        await this.apiClient.getrejectedFilters(
          status,
          receivedfromDate,
          receivedtoDate,
          handOverfromDate,
          handOvertoDate,
        )
      ).result;
    } else {
      response = (
        await this.apiClient.getAllReceivedProductFilters(
          status,
          pSstatus,
          camPstatus,
          camRstatus,
          receivedfromDate,
          receivedtoDate,
          handOverfromDate,
          handOvertoDate,
          resultString,
        )
      ).result;
    }
    const filteredResponse = response?.filter((item: any) => this.deptId.includes(item.divisionId));
    const filteredreject = responsereject?.filter((item: any) =>
      this.deptId.includes(item.divisionId),
    );
    const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    const responeWithIndexreject = filteredreject?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    this.setState(
      {
        searchKey: '',
        onClickMinDate: minDate,
        onClickToDate: toDate,
        currentRecord: responeWithIndex,
        dropDownSelectedRecord: responeWithIndex,
        filteredRecord: responeWithIndex,
        exportvisible: responeWithIndex?.length !== 0 ? true : false,
        rejecteddata: responeWithIndexreject,
        currentRecord1: responeWithIndexreject,
      },
      () => {
        this.setState({
          isRecord: false,
          isRecord1: false,
        });
        this.Filterdata();
      },
    );
  }
  rowSelection = (e: any) => {
    const selectionCount = e.selectedCount;
    this.setState({ selectedRowData: e.selectedRows, selectionCount: selectionCount });
    switch (selectionCount) {
      case 0:
        this.setState({ enableEditButton: false, enableActiveButton: false, ApprovalEnable: true });
        break;
      case 1:
        this.setState({ enableEditButton: true, enableActiveButton: true, ApprovalEnable: true });
        break;
      default:
        this.setState({ enableEditButton: false, enableActiveButton: true });
        break;
    }
    if (selectionCount >= 2) {
      this.setState({ ApprovalEnable: false });
    }
  };

  closingFullImageModal = () => {
    this.setState({ fullImageVisible: false, fullImageSrc: '' });
  };

  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date, downloadFromDate: date });
  };
  handleMinDateChange1 = (date: any) => {
    this.setState({ minDate1: date });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  handleToDateChange = (date: any) => {
    this.setState({ toDate: date, downloadToDate: date });
  };
  handleToDateChange1 = (date: any) => {
    this.setState({ toDate1: date });
  };

  onApprove = async () => {
    try {
      this.setState({ approveVisible: false });
      var itemcode = this.state.row.itemCode;
      var samplestatusrejection = this.state.row.samplestatusrejection;
      var status = 'Approved';
      const response = await this.apiClient.updateQCAction(
        itemcode,
        status,
        samplestatusrejection,
        this.username,
      );
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
      }
      this.refresh();
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  Filterdata = () => {
    if (this.state.selecteddept.length != 0 && this.state.Showrejecteditem == false) {
      var selectedDeptname = this.state.selecteddept.map((selectedDept: any) => selectedDept);
      if (selectedDeptname.length !== this.state.deptOption.length) {
        var ids: any = [];
        var id;
        selectedDeptname.forEach((selectedDept: any) => {
          var option = this.state.deptOption.find((option: any) => {
            return selectedDept === option.text;
          });
          if (option) {
            id = option;
            ids.push(option.key);
          }
        });
        if (ids.length > 0) {
          var record = this.state.currentRecord.filter((e: any) => {
            return ids.includes(e.divisionId);
          });

          this.setState({ filteredRecord: record, currentRecord: record, Dropdownvalue: id });
        }
      } else {
        this.setState({ filteredRecord: this.state.currentRecord });
      }
    } else {
      this.setState({ filteredRecord: this.state.currentRecord });
    }
    if (this.state.selecteddept.length != 0 && this.state.Showrejecteditem == true) {
      var selectedDeptname = this.state.selecteddept.map((selectedDept: any) => selectedDept);
      if (selectedDeptname.length !== this.state.deptOption.length) {
        var ids: any = [];
        var id;
        selectedDeptname.forEach((selectedDept: any) => {
          var option = this.state.deptOption.find((option: any) => {
            return selectedDept === option.text;
          });
          if (option) {
            id = option;
            ids.push(option.key);
          }
        });
        if (ids.length > 0) {
          var record = this.state.currentRecord1.filter((e: any) => {
            return ids.includes(e.divisionId);
          });
          this.setState({ rejecteddata: record });
        }
      }
    } else {
      this.setState({ rejecteddata: this.state.currentRecord1 });
    }
  };

  onReject = async () => {
    try {
      this.setState({ recjectVisible: false });
      var itemcode = this.state.row.itemCode;
      var samplestatusrejection = this.state.row.samplestatusrejection;
      var status = 'Rejected';
      const response = await this.apiClient.updateQCAction(
        itemcode,
        status,
        samplestatusrejection,
        this.username,
      );
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.notify.showSuccessNotify(this.props.t('REJECTED_SUCESSFULLY'));
      }
      this.refresh();
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  showNextImage = () => {
    this.setState((prevState: any) => {
      // Find the index of the current image
      const currentIndex = prevState.imageURL.indexOf(prevState.fullImageSrc);

      // Calculate the next index
      let nextIndex = currentIndex + 1;
      if (nextIndex >= prevState.imageURL.length) {
        nextIndex = 0; // Wrap around to the first image
      }

      // Update the state with the new image
      return {
        currentIndex: nextIndex,
        fullImageSrc: prevState.imageURL[nextIndex],
      };
    });
  };

  showPreviousImage = () => {
    this.setState((prevState: any) => {
      const currentIndex = prevState.imageURL.indexOf(prevState.fullImageSrc);
      let prevIndex = currentIndex - 1;
      if (prevIndex < 0) {
        prevIndex = prevState.imageURL.length - 1;
      }
      return {
        currentIndex: prevIndex,
        fullImageSrc: prevState.imageURL[prevIndex],
      };
    });
  };
  
  render() {
    const { handoverOptions, refreshKey1, divisionSelected } = this.state;
    const { t } = this.props;
    const { deptOption } = this.state;
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.selecteddept.length === deptOption.length
        : false;
    var _columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
        position: 'fixed',
      },
      {
        key: 'itemCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        width: '140px',
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'EANCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        width: '140px',
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
        position: 'fixed',
        selector: (row: any) => {
          if (row.divisionId) {
            var division1 = null;
            const DivKey = parseInt(row.divisionId, 10);
            const statusObject = this.state.deptOption
              ? this.state.deptOption.find((element: any) => element.key === DivKey)
              : null;
            if (statusObject) {
              division1 = statusObject.text;
            }
            return (
              <>
                <span title={division1}>{division1}</span>
              </>
            );
          }
        },
        width: '130px',
      },
      {
        key: 'type',
        name: (
          <Grid container direction={'row'} style={{ display: 'flex', alignItems: 'center' }}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('TYPE')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.ProductImageoptions}
                onChange={(e: any, selectedOption: any) => {
                  this.TypeColText = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    TypeColKey: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter1();
                }}
                selectedKey={this.state.TypeColKey}
                styles={columnFilter}
                style={{ marginLeft: -15, marginTop: 10 }}
              />
              <IconButton
                style={{ marginLeft: -20 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.TypeColText.length != 0 && (
                <IconButton
                  style={{ marginLeft: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.TypeColText = [];
                    this.setState({ TypeColKey: 0 });
                    this.columnFilter1();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => <span title={row.imageType}>{row.imageType}</span>,
        width: '140px',
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'dateReceived',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => (
          <span
            title={
              row.receivedDate != null && row.receivedDate != ''
                ? new Date(row.receivedDate).toLocaleDateString('en-GB')
                : ''
            }
          >
            {row.receivedDate != null &&
              row.receivedDate != '' &&
              new Date(row.receivedDate).toLocaleDateString('en-GB')}
          </span>
        ),
        isVisible: true,
        width: '120px',
        position: 'fixed',
      },
      {
        key: 'image',
        name: <div className={columnHeader}>{`${this.props.t('IMAGE')}`}</div>,
        selector: (row: any) => {
          // const url = this.state.pictureData.find((e: any) => e.id === row.id)?.imageUrl;
          // const value = url?.split('?');
          // const ImgUrl = value == undefined || null ? '' : value[0];
          const isValidUrl = () => {
            try {
              new URL(row.imageUrl);
              return true;
            } catch (e) {
              return false;
            }
          };
          if (row.imageUrl) {
            return (
              <div>
                <a href={isValidUrl() && row.imageUrl} target="_blank" rel="noopener noreferrer">
                  <Icon iconName="Link" title={row.imageUrl} className="linkIcon" />
                </a>
                {/* <Icon
                  iconName="PhotoCollection"
                  title={t('IMAGE')}
                  className="iconStyle"
                  onClick={(e: any) => {
                    window.open(ImgUrl, '_blank');
                  }}
                /> */}
              </div>
            );
          } else {
            return null;
          }
        },
        isVisible: true,
      },
      {
        key: 'psimage',
        name: <div className={columnHeader}>{`${this.props.t('PS_IMAGE')}`}</div>,
        selector: (row: any) => {
          if (row.itemCode) {
            let ImgUrl: string | null = null;
            
            if (row.pictureId !== null && row.pictureId !== '') {
              const pictureIdsArray = row.pictureId
                .split(',')
                .map((id: any) => parseInt(id.trim(), 10));
              const matchingObjects = this.state.pictureData.filter((e: any) =>
                pictureIdsArray.includes(e.id),
              );
              const matchingUrls = matchingObjects.map((obj: any) => obj.imageUrl);
              
              if (matchingUrls.length > 0) {
                ImgUrl = matchingUrls.map((url: string) => url.split('?')[0]).join(',');
              }
            }
      
            if (ImgUrl) {
              return (
                <Icon
                  iconName="PhotoCollection"
                  title={t('COL_PS_IMAGE')}
                  className="iconStyle"
                  onClick={() => {
                    this.setState({
                      imgVisible: true,
                      itemcode: row.itemCode,
                      imageURL: ImgUrl.split(','),
                      imageLoading: ImgUrl === '' ? true : false,
                    });
                  }}
                />
              );
            } else {
              return null;
            }
          } else {
            return null;
          }
        },
        isVisible: true,
        width: '130px',
      },      
      {
        key: 'studioname',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_NAME')}`}</div>,
        selector: (row: any) => <span title={row.studioName}>{row.studioName}</span>,
        isVisible: true,
        width: '140px',
        position: 'fixed',
      },
      {
        key: 'city',
        name: <div className={columnHeader}>{`${this.props.t('COL_CITY')}`}</div>,
        selector: (row: any) => <span title={row.studioLocation}>{row.studioLocation}</span>,
        isVisible: true,
        width: '120px',
        position: 'fixed',
      },
      {
        key: 'totalCapacity',
        name: <div className={columnHeader}>{`${this.props.t('COL_TOTAL_CAPACITY')}`}</div>,
        selector: (row: any) => <span title={row.studioCapacity}>{row.studioCapacity}</span>,
        isVisible: true,
        width: '140px',
        position: 'fixed',
      },
      {
        key: 'allocationdate',
        name: <div className={columnHeader}>{`${this.props.t('STUDIO_ALLOCATION_DATE')}`}</div>,
        selector: (row: any) => (
          <span title={row.studioAllocationDate}>{row.studioAllocationDate}</span>
        ),
        isVisible: true,
        width: '160px',
        position: 'fixed',
      },
      // {
      //   key: 'handoverToBusinessStatus',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_HANDOVER_STATUS')}`}</div>,
      //   // selector: (row: any) => {
      //   //   if (this.state.filteredRecord?.length !== 0) {
      //   //     // if (row.qcaction != null) {
      //   //       var handoverToBusinessStatus;
      //   //       if (row.sampleStatus === 'Approved') {
      //   //         const value = this.state.handoverOptions1.find((e: any) => e.key === 1);
      //   //         if (value) {
      //   //           handoverToBusinessStatus = value.text;
      //   //         }
      //   //         return (
      //   //           <span title={`${handoverToBusinessStatus}`}>{handoverToBusinessStatus}</span>
      //   //         );
      //   //       } else if (row.sampleStatus === 'Rejected') {
      //   //         return (
      //   //           <span className={`customer-badge status-closed`}>
      //   //             <span title={`${'Closed'}`}>{'Closed'}</span>
      //   //           </span>
      //   //         );
      //   //       } else {
      //   //         const value = this.state.handoverOptions1.find((e: any) => e.key === 2);
      //   //         if (value) {
      //   //           handoverToBusinessStatus = value.text;
      //   //         }
      //   //         return (
      //   //           <span title={`${handoverToBusinessStatus}`}>{handoverToBusinessStatus}</span>
      //   //         );
      //   //       }
      //   //     // }
      //   //   }
      //   // },
      //   selector: (row: any) => <span title={row.handoverStatus}>{row.handoverStatus}</span>,
      //   isVisible: true,
      //   position: 'fixed',
      //   width: '130px',
      // },
      {
        key: 'ProductStatus',
        name: <div className={columnHeader}>{`${this.props.t('PRODUCT_STATUS')}`}</div>,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            if (row.qcaction == null) {
              if (row.itemCode) {
                return (
                  <span className={'customer-badge status-Pending'} title={'WIP'}>
                    {'WIP'}
                  </span>
                );
              }
            } else if (row.rejectedcount != null && row.qcaction == null) {
              return (
                <span className={'customer-badge status-Submitted'} title={'Submitted'}>
                  {'Submitted'}
                </span>
              );
            } else if (row.qcaction == 'true') {
              return (
                <span className={'customer-badge status-Submitted'} title={'Submitted'}>
                  {'Submitted'}
                </span>
              );
            }
          }
          return null;
        },
        isVisible: true,
        width: '100px',
        position: 'fixed',
      },
      {
        key: 'sampleRecievedStatus',
        name: (
          <div className={columnHeader}>{`${this.props.t('COL_SAMPLE_RECEIVED_BY_CAM_QC')}`}</div>
        ),
        position: 'fixed',
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            var status;
            if (row.sampleStatus == '1') {
              status = 'Rejected';
            } else if (row.sampleStatus == 'Approved' || row.sampleStatus == '2') {
              status = 'Approved';
            } else if (row.qcaction == 'true' && row.sampleStatus != 'Approved') {
              status = 'Pending';
            } else if (row.rejecteditemsubmit == 'yes' && row.sampleStatus != 'Approved') {
              status = '';
            }
            switch (status) {
              case 'Pending':
                return (
                  <span className={'customer-badge status-Pending'} title="Pending">
                    <span>{'Pending'}</span>
                  </span>
                );
              case 'Approved':
                return (
                  <span className={`customer-badge status-Approved`} title="Approved">
                    <span>{'Approved'}</span>
                  </span>
                );
              case 'Rejected':
                return (
                  <span className={`customer-badge status-Rejected`} title="Rejected">
                    <span>{'Rejected'}</span>
                  </span>
                );
              default:
                return <span></span>;
            }
          }
        },
        isVisible: true,
        width: '130px',
      },
      {
        key: 'handoverDate',
        name: <div className={columnHeader}>{`${this.props.t('COL_HANDOVER_DATE')}`}</div>,
        selector: (row: any) => {
          if (row.sampleStatus === 'Rejected') {
            return null;
          } else {
            const formattedDate =
              row.statusDate != null && row.statusDate !== '0001-01-01T00:00:00'
                ? new Date(row.statusDate).toLocaleDateString('en-GB')
                : null;
            return <span title={formattedDate != null ? formattedDate : ''}>{formattedDate}</span>;
          }
        },
        isVisible: true,
        width: '140px',
        position: 'fixed',
      },
      {
        key: 'CAMQCL1ACK',
        name: <div className={columnHeader}>{`${this.props.t('CAMQC_STATUS')}`}</div>,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            let Classname;
            if (row.camQCStatus == 'Pending' && row.camqcl1action == null) {
              camqcl1action = 'Pending';
              Classname = 'Pending';
            } else if (row.camqcl1action == 'Received') {
              camqcl1action = 'Received';
              Classname = 'Submitted';
            } else if (row.productreceived == '1' || row.camqcl1action == '1') {
              camqcl1action = 'Received';
              Classname = 'Submitted';
            } else if (row.sampleStatus == 'Approved' || row.sampleStatus == '2') {
              camqcl1action = 'Closed';
              Classname = 'Closed';
            } else if (row.sampleStatus == '1') {
              camqcl1action = 'Closed';
              Classname = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + Classname} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
          return null;
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'CAMQCL1Check',
        name: <div className={columnHeader}>{`${this.props.t('CAMQC_ACTION_STATUS')}`}</div>,
        // selector: (row: any) => row.camqcl1qualitycheck,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            if (row.camqcl1action == 'Received' && row.camqcl1qualitycheck == null) {
              camqcl1action = 'Pending';
              if (row.productreceived == '1') {
                camqcl1action = 'Approved';
              }
            } else if (row.productreceived == '1') {
              camqcl1action = 'Approved';
            } else if (row.camqcl1qualitycheck == 'Pass') {
              camqcl1action = 'Approved';
            } else if (row.camqcl1action == '1') {
              camqcl1action = 'Rejected';
            } else if (row.sampleStatus == '2' || row.sampleStatus == '1') {
              camqcl1action = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + camqcl1action} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
          return null;
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'CAMQCL1Submission',
        name: <div className={columnHeader}>{`${this.props.t('CAMQCL1_SUBMITION')}`}</div>,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            if (row.camqcl1qualitycheck == 'Pass' && row.camQCStatus != 'Yes') {
              camqcl1action = 'WIP';
            } else if (row.camQCStatus == 'Yes') {
              camqcl1action = 'Submitted';
            } else if (row.productreceived == '1') {
              camqcl1action = 'Submitted';
            } else if (row.sampleStatus == '2' || row.sampleStatus == '1') {
              camqcl1action = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + camqcl1action} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'PhotoshootAction',
        name: <div className={columnHeader}>{`${this.props.t('PHOTOSHOOT_ACTION')}`}</div>,
        // selector: (row: any) => row.productreceived,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            if (row.camQCStatus == 'Yes' && row.productreceived == null) {
              camqcl1action = 'Pending';
            } else if (row.productreceived == 'Received') {
              camqcl1action = 'Approved';
            } else if (row.productreceived == '1') {
              camqcl1action = 'Rejected';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1'
            ) {
              camqcl1action = 'Closed';
            } else if (row.sampleStatus == '1') {
              camqcl1action = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + camqcl1action} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
          return null;
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'productStatus',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('COL_PRODUCT_STATUS')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.PTstatus = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    PSstatusopt: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter1();
                }}
                selectedKey={this.state.PSstatusopt}
                styles={columnFilter}
                style={{ marginLeft: -22, marginTop: 18 }}
              />
              <IconButton
                style={{ marginLeft: -27, marginTop: 8 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.PTstatus.length != 0 && (
                <IconButton
                  style={{ marginLeft: -3, marginTop: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.PTstatus = [];
                    this.setState({ PSstatusopt: 0 });
                    this.columnFilter1();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          if (this.state.filteredRecord != null || this.state.filteredRecord != undefined) {
            if (row.photoShootStatus === 'Rejected') {
              return (
                <span className={`customer-badge status-Rejected`} title="Rejected">
                  <span>{'Rejected'}</span>
                </span>
              );
            } else if (row.photoShootStatus === 'Completed') {
              return (
                <span className={`customer-badge status-Approved`} title="Completed">
                  {'Completed'}
                </span>
              );
            } else if (row.photoShootStatus === 'Pending') {
              return (
                <span className={'customer-badge status-Pending'} title="Pending">
                  <span>{'Pending'}</span>
                </span>
              );
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1' &&
              row.productreceived == '1'
            ) {
              return (
                <span title="Closed" className={'customer-badge status-Closed'}>
                  {'Closed'}
                </span>
              );
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1'
            ) {
              return (
                <span title="Closed" className={'customer-badge status-Closed'}>
                  {'Closed'}
                </span>
              );
            } else if (row.sampleStatus == '1') {
              return (
                <span title="Closed" className={'customer-badge status-Closed'}>
                  {'Closed'}
                </span>
              );
            } else {
              return <span></span>;
            }
          }
        },
        isVisible: true,
        minWidth: '130px',
        position: 'fixed',
      },
      {
        key: 'CAMQCL2Check',
        name: <div className={columnHeader}>{`${this.props.t('CAMQCL2_STATUS')}`}</div>,
        // selector: (row: any) => row.qualitycheck,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            if (row.photoShootStatus === 'Completed' && row.qualitycheck == null) {
              camqcl1action = 'Pending';
            } else if (row.qualitycheck == 'Pass') {
              camqcl1action = 'Approved';
            } else if (row.qualitycheck == '1' && row.productreceived != '1') {
              camqcl1action = 'Rejected';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1' &&
              row.productreceived == '1'
            ) {
              camqcl1action = 'Closed';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1'
            ) {
              camqcl1action = 'Closed';
            } else if (row.sampleStatus == '1') {
              camqcl1action = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + camqcl1action} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
          return null;
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'CAMQCL2Submission',
        name: <div className={columnHeader}>{`${this.props.t('CAMQCL2_SUBMITION')}`}</div>,
        // selector: (row: any) => row.camqcl2submit,
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            let camqcl1action;
            if (row.qualitycheck == 'Pass' && row.camqcl2submit == null) {
              camqcl1action = 'WIP';
            } else if (row.camqcl2submit == '1') {
              camqcl1action = 'Submitted';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1' &&
              row.productreceived == '1'
            ) {
              camqcl1action = 'Closed';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1'
            ) {
              camqcl1action = 'Closed';
            } else if (row.sampleStatus == '1') {
              camqcl1action = 'Closed';
            } else if (row.qualitycheck == '1') {
              camqcl1action = 'Closed';
            } else {
              return null;
            }
            return (
              <span className={'customer-badge status-' + camqcl1action} title={camqcl1action}>
                {camqcl1action}
              </span>
            );
          }
        },
        isVisible: true,
        width: '130px',
        position: 'fixed',
      },
      {
        key: 'stock_return_to_qc',
        name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
        isVisible: true,
        minWidth: '160px',
        position: 'fixed',
        selector: (row: any) => {
          if (this.state.filteredRecord !== null || this.state.filteredRecord !== undefined) {
            var stockreturn = row.stockreturn;
            if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1' &&
              row.productreceived == '1'
            ) {
              stockreturn = '2';
            } else if (
              (row.sampleStatus == '2' || row.sampleStatus == 'Approved') &&
              row.camqcl1action == '1'
            ) {
              stockreturn = '2';
            } else if (row.sampleStatus == '1') {
              stockreturn = '2';
            }
            switch (stockreturn) {
              case '0':
                return (
                  <span className={'customer-badge status-Pending'} title="Yet To Return">
                    {'Yet To Return'}
                  </span>
                );
              case '1':
                return (
                  <>
                    <span className={'customer-badge status-Approved'} title="Returned">
                      {'Returned'}
                    </span>
                    <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
                      {row.comments != null ? (
                        <FaRegClone
                          onClick={() => {
                            this.setState({
                              returnstatus: true,
                              returnvalue: row.comments,
                            });
                          }}
                        />
                      ) : (
                        ''
                      )}
                    </span>
                  </>
                );
              case '2':
                return (
                  <span className={'customer-badge status-Closed'} title="Yet To Return">
                    {'Closed'}
                  </span>
                );
              default:
                return <span></span>;
            }
          }
        },
      },
      {
        key: 'Website_Live_status',
        name: <div className={columnHeader}>{`${this.props.t('WEBSITE_LIVE_STATUS')}`}</div>,
        selector: (row: any) => <span>{}</span>,
        isVisible: true,
        width: '135px',
      },
      {
        key: 'Website_Live_date',
        name: <div className={columnHeader}>{`${this.props.t('WEBSITE_LIVE_DATE')}`}</div>,
        selector: (row: any) => <span>{}</span>,

        isVisible: true,
        width: '135px',
      },
    ]; //?.filter((item) => item.isVisible);
    var _columns_item: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
        position: 'fixed',
      },
      {
        key: 'itemCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        width: '140px',
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'EANCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        width: '140px',
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
        position: 'fixed',
        selector: (row: any) => {
          if (row.divisionId) {
            var division1 = null;
            const DivKey = parseInt(row.divisionId, 10);
            const statusObject = this.state.deptOption
              ? this.state.deptOption.find((element: any) => element.key === DivKey)
              : null;
            if (statusObject) {
              division1 = statusObject.text;
            }
            return (
              <>
                <span title={division1}>{division1}</span>
              </>
            );
          }
        },
        width: '130px',
      },
      {
        key: 'type',
        name: (
          <Grid container direction={'row'} style={{ display: 'flex', alignItems: 'center' }}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('TYPE')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.ProductImageoptions}
                onChange={(e: any, selectedOption: any) => {
                  this.TypeColText = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    TypeColKey: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter1();
                }}
                selectedKey={this.state.TypeColKey}
                styles={columnFilter}
                style={{ marginLeft: -15, marginTop: 10 }}
              />
              <IconButton
                style={{ marginLeft: -20 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.TypeColText.length != 0 && (
                <IconButton
                  style={{ marginLeft: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.TypeColText = [];
                    this.setState({ TypeColKey: 0 });
                    this.columnFilter1();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => <span title={row.imageType}>{row.imageType}</span>,
        width: '140px',
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'dateReceived',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => {
          if (row.receivedDate != null && row.receivedDate != '') {
            const formattedDate = new Date(row.receivedDate).toLocaleDateString('en-GB');
            return <span title={formattedDate}>{formattedDate}</span>;
          }
          return <span></span>;
        },
        isVisible: true,
        width: '120px',
        position: 'fixed',
      },
      // {
      //   key: 'productStatus',
      //   name: (
      //     <Grid container direction={'row'}>
      //       <Grid item lg={8} xs={9}>
      //         <div className={columnHeader}>{`${this.props.t('COL_PRODUCT_STATUS')}`}</div>
      //       </Grid>
      //       <Grid item lg={2} xs={3}>
      //         <ComboBox
      //           className="dropdown"
      //           options={this.state.PSstatus}
      //           onChange={(e: any, selectedOption: any) => {
      //             this.PTstatus = selectedOption.text;
      //             this.setState({
      //               isDivision: true,
      //               PSstatusopt1: selectedOption.key,
      //               searchKey: '',
      //             });
      //             this.columnFilter1();
      //           }}
      //           selectedKey={this.state.PSstatusopt1}
      //           styles={columnFilter}
      //           style={{ marginLeft: -22, marginTop: 18 }}
      //         />
      //         <IconButton
      //           style={{ marginLeft: -27, marginTop: 8 }}
      //           styles={filterButton}
      //           iconProps={filterIcon}
      //           title={'Filter'}
      //           ariaLabel="Close popup modal"
      //         />
      //       </Grid>
      //       <Grid item lg={1} xs={1}>
      //         {this.PTstatus.length != 0 && (
      //           <IconButton
      //             style={{ marginLeft: -3, marginTop: 8 }}
      //             title={'Clear Filter'}
      //             styles={filterButton}
      //             iconProps={clearFilter}
      //             onClick={() => {
      //               this.PTstatus = [];
      //               this.setState({ PSstatusopt1: 0 });
      //               this.columnFilter1();
      //             }}
      //           />
      //         )}
      //       </Grid>
      //     </Grid>
      //   ),
      //   selector: (row: any) => {
      //     if (this.state.rejecteddata !== null && this.state.rejecteddata !== undefined) {
      //       if (row.photoShootStatus === 'Rejected') {
      //         return (
      //           <span className={`customer-badge status-Rejected`}>
      //             <span title={`${'Rejected'}`}>{'Rejected'}</span>
      //           </span>
      //         );
      //       } else {
      //         if (row.photoShootStatus === 'Completed') {
      //           return <span className={`customer-badge status-Approved`}>{'Completed'}</span>;
      //         } else {
      //           if (row.photoShootStatus === 'Pending') {
      //             return (
      //               <span className={'customer-badge status-Pending'}>
      //                 <span title={`${'Pending'}`}>{'Pending'}</span>
      //               </span>
      //             );
      //           } else {
      //             return <span></span>;
      //           }
      //         }
      //       }
      //     }
      //   },
      //   isVisible: true,
      //   position: 'fixed',
      //   minWidth: '130px',
      // },
      // {
      //   key: 'stock_return_to_qc',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
      //   isVisible: true,
      //   position: 'fixed',
      //   minWidth: '120px',
      //   selector: (row: any) => {
      //     if (this.state.rejecteddata !== null && this.state.rejecteddata !== undefined) {
      //       if (this.state.rejecteddata !== null && this.state.rejecteddata !== undefined) {
      //         switch (row.stockreturn) {
      //           case '0':
      //             return <span title={`${'Yet To Return'}`}>{'Yet To Return'}</span>;
      //           case '1':
      //             return (
      //               <>
      //                 <span title={`${'Returned'}`}>{'Returned'}</span>
      //                 <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
      //                   {row.comments != null ? (
      //                     <FaRegClone
      //                       onClick={() => {
      //                         this.setState({
      //                           returnstatus: true,
      //                           returnvalue: row.comments,
      //                         });
      //                       }}
      //                     />
      //                   ) : (
      //                     ''
      //                   )}
      //                 </span>
      //               </>
      //             );
      //           default:
      //             return <span></span>;
      //         }
      //       }
      //     }
      //   },
      // },
      {
        key: 'rejectedby',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_BY')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          if (
            this.state.rejecteddata !== null &&
            this.state.rejecteddata !== undefined &&
            row.rejectedby !== undefined
          ) {
            const rejectedByArray = row.rejectedby.split(', ');
            const removeParentheses = (value: string) => {
              return value.replace(/\s*\(.*?\)\s*/g, '').trim();
            };
            const lastRejectedBy = removeParentheses(rejectedByArray[0]);
            return (
              <span>
                <span
                  title={'Click to view rejected history'}
                  onClick={() => {
                    const formattedData: any[] = [];
                    if (row) {
                      const rejectedByArray = row.rejectedby.split(', ');
                      const rejectedReasonArray = row.rejectedreason.split(', ');
                      const rejectedDateArray = row.rejecteddate.split(', ');
                      rejectedByArray.forEach((rejectedBy: any, index: any) => {
                        const rejectedReason = rejectedReasonArray[index];
                        const rejectedDate = rejectedDateArray[index];
                        const match = rejectedBy.match(/^(.+?)\s*\((.+?)\)$/);
                        let rejectedByName = rejectedBy;
                        let rejectedRole = '';
                        if (match) {
                          rejectedByName = match[1].trim();
                          rejectedRole = match[2].trim();
                        }
                        const rejectionEntry = {
                          rejectedby: rejectedByName,
                          rejectedrole: rejectedRole,
                          rejectedreason: rejectedReason,
                          rejecteddate: rejectedDate,
                        };
                        formattedData.push(rejectionEntry);
                      });
                    }
                    this.setState({ Rowdata: formattedData }, () => {
                      this.setState({ Rejectedhistory: true, addConfirm: true });
                    });
                  }}
                >
                  {lastRejectedBy}
                </span>
              </span>
            );
          }
        },
      },
      {
        key: 'roless',
        name: <div className={columnHeader}>{`${this.props.t('COL_ROLES')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          if (
            this.state.rejecteddata !== null &&
            this.state.rejecteddata !== undefined &&
            row.rejectedby !== undefined
          ) {
            const rejectedByArray = row.rejectedby.split(', ');
            const extractInsideParentheses = (value: string) => {
              const match = value.match(/\(([^)]+)\)/);
              return match ? match[1] : '';
            };
            const lastRejectedrole = extractInsideParentheses(
              rejectedByArray[0],
            );
            return (
              <span>
                <span
                  title={'Click to view rejected history'}
                  onClick={() => {
                    const formattedData: any[] = [];
                    if (row) {
                      const rejectedByArray = row.rejectedby.split(', ');
                      const rejectedReasonArray = row.rejectedreason.split(', ');
                      const rejectedDateArray = row.rejecteddate.split(', ');
                      rejectedByArray.forEach((rejectedBy: any, index: any) => {
                        const rejectedReason = rejectedReasonArray[index];
                        const rejectedDate = rejectedDateArray[index];
                        const match = rejectedBy.match(/^(.+?)\s*\((.+?)\)$/);
                        let rejectedByName = rejectedBy;
                        let rejectedRole = '';
                        if (match) {
                          rejectedByName = match[1].trim();
                          rejectedRole = match[2].trim();
                        }
                        const rejectionEntry = {
                          rejectedby: rejectedByName,
                          rejectedrole: rejectedRole,
                          rejectedreason: rejectedReason,
                          rejecteddate: rejectedDate,
                        };
                        formattedData.push(rejectionEntry);
                      });
                    }
                    this.setState({ Rowdata: formattedData }, () => {
                      this.setState({ Rejectedhistory: true, addConfirm: true });
                    });
                  }}
                >
                  {lastRejectedrole}
                </span>
              </span>
            );
          }
        },
      },
      {
        key: 'rejectedreason',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_REASON')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          if (
            this.state.rejecteddata !== null &&
            this.state.rejecteddata !== undefined &&
            row.rejectedreason !== undefined
          ) {
            const rejectedReasonArray = row.rejectedreason.split(', ');
            const lastRejectedReason = rejectedReasonArray[0];
            return (
              <span>
                <span
                  title={'Click to view rejected history'}
                  onClick={() => {
                    const formattedData: any[] = [];
                    if (row) {
                      const rejectedByArray = row.rejectedby.split(', ');
                      const rejectedReasonArray = row.rejectedreason.split(', ');
                      const rejectedDateArray = row.rejecteddate.split(', ');
                      rejectedByArray.forEach((rejectedBy: any, index: any) => {
                        const rejectedReason = rejectedReasonArray[index];
                        const rejectedDate = rejectedDateArray[index];
                        const match = rejectedBy.match(/^(.+?)\s*\((.+?)\)$/);
                        let rejectedByName = rejectedBy;
                        let rejectedRole = '';
                        if (match) {
                          rejectedByName = match[1].trim();
                          rejectedRole = match[2].trim();
                        }
                        const rejectionEntry = {
                          rejectedby: rejectedByName,
                          rejectedrole: rejectedRole,
                          rejectedreason: rejectedReason,
                          rejecteddate: rejectedDate,
                        };
                        formattedData.push(rejectionEntry);
                      });
                    }
                    this.setState({ Rowdata: formattedData }, () => {
                      this.setState({ Rejectedhistory: true, addConfirm: true });
                    });
                  }}
                >
                  {lastRejectedReason}
                </span>
              </span>
            );
          }
        },
      },
      {
        key: 'rejectedcount',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_COUNT')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          if (this.state.rejecteddata !== null && this.state.rejecteddata !== undefined) {
            return (
              <span>
                <span
                  title={'Click to view rejected history'}
                  onClick={() => {
                    const formattedData: any[] = [];
                    if (row) {
                      const rejectedByArray = row.rejectedby.split(', ');
                      const rejectedReasonArray = row.rejectedreason.split(', ');
                      const rejectedDateArray = row.rejecteddate.split(', ');
                      rejectedByArray.forEach((rejectedBy: any, index: any) => {
                        const rejectedReason = rejectedReasonArray[index];
                        const rejectedDate = rejectedDateArray[index];
                        const match = rejectedBy.match(/^(.+?)\s*\((.+?)\)$/);
                        let rejectedByName = rejectedBy;
                        let rejectedRole = '';
                        if (match) {
                          rejectedByName = match[1].trim();
                          rejectedRole = match[2].trim();
                        }
                        const rejectionEntry = {
                          rejectedby: rejectedByName,
                          rejectedrole: rejectedRole,
                          rejectedreason: rejectedReason,
                          rejecteddate: rejectedDate,
                        };
                        formattedData.push(rejectionEntry);
                      });
                    }
                    this.setState({ Rowdata: formattedData }, () => {
                      this.setState({ Rejectedhistory: true, addConfirm: true });
                    });
                  }}
                >
                  {row.rejectedcount}
                </span>
              </span>
            );
          }
        },
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${this.props.t('CUL_STATUS')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          if (this.state.rejecteddata.length > 0){
          if (this.state.rejecteddata !== null && this.state.rejecteddata !== undefined) {
            return (
              <span>
                <span
                  title={
                    row.rejecteditemsubmit == null
                      ? `${this.props.t('REJECTED')}`
                      : `${this.props.t('BTN_SUBMITED')}`
                  }
                >
                  {row.rejecteditemsubmit == null
                    ? `${this.props.t('REJECTED')}`
                    : `${this.props.t('BTN_SUBMITED')}`}
                </span>
              </span>
            );
          }
        }
        },
      },
    ];

    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [..._columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    const columnreject: any = [
      {
        key: 'rejectedby',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_BY')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          return (
            <span>
              <span title={row.rejectedby}>{row.rejectedby}</span>
            </span>
          );
        },
      },
      {
        key: 'Roles',
        name: <div className={columnHeader}>{`${this.props.t('COL_ROLES')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          return (
            <span>
              <span title={row.rejectedrole}>{row.rejectedrole}</span>
            </span>
          );
        },
      },
      {
        key: 'rejectedreason',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_REASON')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          return (
            <span>
              <span title={row.rejectedreason}>{row.rejectedreason}</span>
            </span>
          );
        },
      },
      {
        key: 'rejectedcount',
        name: <div className={columnHeader}>{`${this.props.t('COL_REJECTED_DATE')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        position: 'fixed',
        selector: (row: any) => {
          return (
            <span>
              <span title={row.rejecteddate}>{row.rejecteddate}</span>
            </span>
          );
        },
      },
    ];
    const handleColumnReorder1 = (newColumns: any) => {
      if (_columns_item == undefined) {
        var _columns_item = newColumns;
        var _columns_item: any = [..._columns_item];
        if (newColumns == this.state.currentColumns1) {
          this.setState({ currentColumns1: _columns_item });
        } else {
          this.setState({ currentColumns1: newColumns });
        }
      } else {
        var _columns_item: any = [..._columns_item];
        if (newColumns == this.state.currentColumns1) {
          this.setState({ currentColumns1: _columns_item });
        } else {
          this.setState({ currentColumns1: newColumns });
        }
      }
    };
    return (
      <div className="layout width-100">
        <Stack>
          <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
            'COL_PHOTOSHOOT_TRACKING',
          )}`}</Text>
        </Stack>
        <div className="ReceivedClassProduct" />
        <Grid lg={12} item container spacing={0} direction={'row'}>
          {this.roleId.includes(13) ||
          this.roleId.includes(7) ||
          this.roleId.includes(1) ||
          this.roleId.includes(16) ? (
            <Grid item lg={2} xs={5}>
              <DefaultButton
                className="button"
                text={`${t('BTN_RECEIVED_PRODUCT')}`}
                styles={this.state.showrecivedprd == true ? PIMHeaderButtonsQc : PIMButtonsQc}
                onClick={() => {
                  this.rejected = false;
                  this.setState(
                    {
                      showActionColumn: false,
                      showrecivedprd: true,
                      Showrejecteditem: false,
                    },
                    () => {
                      this.refresh();
                    },
                  );
                }}
              />
            </Grid>
          ) : (
            ''
          )}
          {this.roleId.includes(13) ||
          this.roleId.includes(7) ||
          this.roleId.includes(1) ||
          this.roleId.includes(16) ? (
            <Grid item lg={2} xs={5}>
              <DefaultButton
                className="Qcbutton"
                text={`${t('COL_PHOTOSHOOT_TRACKING')}`}
                styles={this.state.showActionColumn == true ? PIMHeaderButtonsQc : PIMButtonsQc}
                onClick={() => {
                  this.rejected = false;
                  this.setState({
                    showActionColumn: true,
                    Showrejecteditem: false,
                    showrecivedprd: false,
                  });
                }}
              />
            </Grid>
          ) : (
            ''
          )}
          {this.roleId.includes(13) ||
          this.roleId.includes(7) ||
          this.roleId.includes(1) ||
          this.roleId.includes(16) ? (
            <Grid item lg={2} xs={5}>
              <DefaultButton
                className="QCItemreject"
                text={`${t('REJECTED_ITEMCODE')}`} //Rejected Itemcode
                styles={this.state.Showrejecteditem == true ? PIMHeaderButtonsQc : PIMButtonsQc}
                onClick={() => {
                  this.rejected = true;
                  this.setState(
                    {
                      Showrejecteditem: true,
                      showrecivedprd: false,
                      showActionColumn: false,
                    },
                    () => {
                      this.refresh();
                    },
                  );
                }}
              />
            </Grid>
          ) : (
            ''
          )}
          <Grid
            item
            lg={
              this.roleId.includes(13) ||
              this.roleId.includes(7) ||
              this.roleId.includes(1) ||
              this.roleId.includes(16)
                ? 5.6
                : 11.6
            }
            xs={1}
          />
          <Grid item lg={0.3} xs={1}>
            {this.state.showActionColumn == false && (
              <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle"
                onClick={() => {
                  if (
                    this.state.filteredRecord &&
                    this.state.filteredRecord.length !== 0 &&
                    this.state.Showrejecteditem == false
                  ) {
                    this.downloadFile(
                      this.state.receivedfromDate,
                      this.state.receivedtoDate,
                      this.state.handOverfromDate,
                      this.state.handOvertoDate,
                      this.state.pSstatus,
                      this.state.division,
                    );
                  } else if (
                    this.state.rejecteddata &&
                    this.state.rejecteddata.length !== 0 &&
                    this.state.Showrejecteditem == true
                  ) {
                    this.downloadFileRejected(
                      this.state.downloadFromDate,
                      this.state.downloadToDate,
                    );
                  }
                }}
              />
            )}
          </Grid>
        </Grid>
        {/* <br /> */}
        <Grid lg={12} item container spacing={1.5} direction={'row'}>
          {this.state.showActionColumn == false ? (
            <Grid lg={12} item container spacing={1.5} direction={'row'}>
              <Grid item lg={1.9} xs={6}>
                <SearchBox
                  name="search"
                  className="searchBox"
                  styles={PIMsearchBoxStyles}
                  placeholder={`${t('ITEMCODE')}`}
                  onChange={(e: any) => this.onSearch(e)}
                  value={this.state.searchKey}
                  onClear={() => this.setState({ searchKey: '' })}
                />
              </Grid>
              <Grid item lg={1.8} xs={6}>
                {this.state.showActionColumn ? (
                  ''
                ) : (
                  // <Dropdown
                  //   className="column-Topdivision-option dropdown"
                  //   placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                  //   options={this.state.deptOption}
                  //   key={this.state.refreshKey}
                  //   defaultSelectedKey={1}
                  //   styles={PIMdropdownStylesDd}
                  //   onChange={(event, item) => {
                  //     if (item) {
                  //       this.setState({ dropdown: item, DropdownVal: item.text }, () => {
                  //         this.Filterdata();
                  //       });
                  //     }
                  //   }}
                  // />
                  <ComboBox
                    multiSelect
                    placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                    styles={PIMdropdownStylesDd}
                    options={[
                      { key: 'select-toggle', text: 'Select All', selected: selecteddept },
                      ...(deptOption || []).map((option: any) => ({
                        key: option.key,
                        text: option.text,
                        selected: Array.isArray(this.state.selecteddept)
                          ? this.state.selecteddept.includes(option.text)
                          : false,
                      })),
                    ]}
                    key={this.state.refreshKey}
                    onChange={(e: any, selectedOption: any) => {
                      if (selectedOption && selectedOption.key === 'select-toggle') {
                        const allSelected =
                          (this.state.selecteddept || []).length === (deptOption || []).length;
                        const newSelectedSeasons = allSelected
                          ? []
                          : deptOption
                              .map((option: any) =>
                                option.text === 'Select All' ? null : option.text,
                              )
                              .filter(Boolean);
                        this.setState(
                          {
                            selecteddept: newSelectedSeasons,
                            searchKey: '',
                            // divisionSelected: ''
                          },
                          // () => {
                          //   this.Filterdata();
                          // },
                        );
                      } else {
                        const updatedSelectedSeasons = Array.isArray(this.state.selecteddept)
                          ? [...this.state.selecteddept]
                          : [];
                        const isSelected = Array.isArray(this.state.selecteddept)
                          ? this.state.selecteddept.includes(selectedOption.text)
                          : false;
                        if (isSelected) {
                          const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                          updatedSelectedSeasons.splice(index, 1);
                        } else {
                          updatedSelectedSeasons.push(selectedOption.text);
                        }
                        this.setState(
                          {
                            selecteddept: updatedSelectedSeasons,
                            // divisionSelected: '',
                            searchKey: '',
                          },
                          // () => {
                          //   this.Filterdata();
                          // },
                        );
                      }
                    }}
                    selectedKey={this.state.selecteddept || []}
                  />
                )}
              </Grid>

              <Grid item lg={1.9} xs={6}>
                <CommonDate
                  onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                  onToDateChange={(e: any) => this.handleToDateChange(e)}
                  ref={this.commonDateRefs[0]}
                  label={`${t('RECEIVED_DATE')}`}
                />
                {/* placeholder={t('RECEIVED_DATE_PALCEHOLDER')} */}
              </Grid>

              {this.state.Showrejecteditem == false && (
                <>
                  <Grid item lg={1.9} xs={6}>
                    {/* placeholder={t('HANDOVER_DATE_PLACEHOLDER')}                           */}
                    <CommonDate
                      onMinDateChange={(e: any) => this.handleMinDateChange1(e)}
                      onToDateChange={(e: any) => this.handleToDateChange1(e)}
                      ref={this.commonDateRefs[1]}
                      label={`${t('COL_HANDOVER_DATE')}`}
                    />
                  </Grid>

                  <Grid item lg={1.5} xs={6}>
                    <Dropdown
                      placeholder={t('STATUS')}
                      options={[
                        {
                          key: 'select-toggle',
                          text: 'Select All',
                          selected:
                            this.state.divisionSelected?.length ||
                            0 === (handoverOptions?.length || 0),
                        },
                        ...(handoverOptions || []).map((option: any) => ({
                          key: option.key,
                          text: option.text,
                          selected: (this.state.divisionSelected || []).includes(option.key),
                        })),
                      ]}
                      key={refreshKey1}
                      multiSelect
                      onChange={this.handleDropdownChange}
                      selectedKeys={this.state.divisionSelected}
                      styles={PIMdropdownStyles21}
                    />
                  </Grid>
                </>
              )}
              {this.state.showActionColumn || (this.roleId.includes(2) && this.rolelvl == 'Level 2')
                ? ''
                : this.state.Showrejecteditem == false && (
                    <Grid item lg={0.5} xs={6}>
                      {/* <Icon
                        iconName="CircleAddition"
                        title={this.props.t('ROLE_USER_BTN_ADD')}
                        className="iconStyle"
                        onClick={() => {
                          // if (this.state.selecteddept.length == 1) {
                          this.setState({ A_D_Visible: true, selectedRowData: [] });
                          // }
                        }}
                      /> */}
                    </Grid>
                  )}
              {this.state.showActionColumn ||
              (this.roleId.includes(2) && this.rolelvl == 'Level 2') ? (
                ''
              ) : (
                <Grid item lg={0.5} xs={6}>
                  {/* <Icon
                    iconName="WaitlistConfirm"
                    title={this.props.t('ADD_BTN_SUBMIT')}
                    className="iconStyle"
                    onClick={() => {
                      if (this.state.selectedRowData != '') {
                        this.onApproveBatch();
                      }
                    }}
                  /> */}
                </Grid>
              )}

              {this.state.showActionColumn || (this.roleId.includes(2) && this.rolelvl == 'Level 2') ? (
                <Grid item lg={1.0} xs={6} />
              ) : ''}

              {this.state.Showrejecteditem == true && (
                <>
                  <Grid item lg={1.51} xs={6} />
                  <Grid item lg={1.9} xs={6} />
                </>
              )}
              {this.state.showActionColumn || (this.roleId.includes(2) && this.rolelvl == 'Level 2')
                ? ''
                : this.state.Showrejecteditem == true && <Grid item lg={0.49} xs={2} />}
              <Grid item lg={0.47} xs={2} />
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Search"
                  title={this.props.t('BTN_SEARCH')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.toDate != null ||
                      this.state.toDate1 != null ||
                      this.state.minDate != null ||
                      this.state.minDate1 != null ||
                      this.state.selecteddept.length != 0 ||
                      this.state.divisionSelected != ''
                    ) {
                      this.onFilterClick();
                    }
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Clear"
                  title={t('BTN_CLEAR')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.toDate != null ||
                      this.state.toDate1 != null ||
                      this.state.minDate != null ||
                      this.state.minDate1 != null ||
                      this.state.selecteddept.length != 0 ||
                      this.state.searchKey != '' ||
                      this.state.divisionSelected != ''
                    ) {
                      this.setState({
                        fromDate: null,
                        toDate: null,
                        searchKey: '',
                        downloadToDate: null,
                        downloadFromDate: null,
                      });
                      this.refresh();
                      this.resetDates();
                    }
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Refresh"
                  title={this.props.t('BTN_REFRESH')}
                  className="iconStyle"
                  onClick={() => {
                    this.setState(
                      {
                        divisionSelected: undefined,
                        dropdown: 'All',
                        downloadToDate: null,
                        downloadFromDate: null,
                      },
                      () => {
                        this.forceUpdate(); // Force component update filteredRecord: [],
                        this.refresh();
                      },
                    );
                  }}
                />
              </Grid>
            </Grid>
          ) : (
            ''
          )}
          {this.state.showActionColumn == true ? <PhotoShootTracking /> : ''}
        </Grid>
        <Modal
          isOpen={this.state.recjected1}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ recjected1: false })}
          isBlocking={false}
        >
          <div className="statusCommentscancel">
            {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ recjected1: false });
              }}
            />
          </div>
          <div className="statusComments">
            <form noValidate autoComplete="off">
              <TextField
                styles={rejectComment}
                className="textField"
                label={`${this.props.t('comment')}`}
                value={this.state.descriptionValue1}
                name="comment"
                required
                resizable={false}
                multiline
                autoAdjustHeight
                data-attribute-name="Name"
                data-async
              />
            </form>
          </div>
        </Modal>

        <Modal
          isOpen={this.state.returnstatus}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ returnstatus: false })}
          isBlocking={false}
        >
          <div className="statusCommentscancel">
            {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ returnstatus: false });
              }}
            />
          </div>
          <div className="statusComments">
            <form noValidate autoComplete="off">
              <TextField
                styles={rejectComment}
                className="textField"
                label={`${this.props.t('comment')}`}
                value={this.state.returnvalue}
                name="comment"
                required
                resizable={false}
                multiline
                autoAdjustHeight
                data-attribute-name="Name"
                data-async
              />
            </form>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.Rejectedhistory}
          containerClassName={PIMcontentStyles.tableContainerReject}
          onDismiss={() => this.setState({ Rejectedhistory: false })}
          isBlocking={false}
        >
          <div className="flex-container">
            <span className="Rejected">{t('REJECTED_HISTORY')}</span>
          </div>
          <div className="statusCommentscancel">
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ Rejectedhistory: false });
              }}
            />
          </div>
          <div className={PIMcontentStyles.body} style={{ marginBottom: '0%' }}></div>
          <div className="layout width-90 ">
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={12} xs={12}>
                {/* <div className="dataTableFixedFooter"> */}
                <DataTable
                  columns={columnreject}
                  data={this.state.Rowdata}
                  pagination={true}
                  selectableRowsHighlight={
                    this.state.selectableRowsHighlight == false ? false : true
                  }
                  selectableRowsVisibleOnly={this.state.selectedRowData}
                  clearSelectedRows={this.state.noSelection}
                  // onSelectedRowsChange={this.rowSelection}
                  onColumnOrderChange={handleColumnReorder}
                  highlightOnHover
                  fixedHeader
                  fixedHeaderScrollHeight="58vh"
                  striped
                  paginationComponentOptions={{
                    rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}`,
                  }}
                  paginationPerPage={this.DefaultperpageReject()}
                  paginationRowsPerPageOptions={this.perpageReject()}
                  noDataComponent={
                    <div className="noDataWidth">
                      {<DataTable columns={_columns} data={[{ '': '' }]} />}
                      <Stack className="noRecordsWidth">
                        {this.state.isRecord == true
                          ? `${this.props.t('RECORDS')}`
                          : `${this.props.t('NO_RECORDS')}`}
                      </Stack>
                    </div>
                  }
                />
                {/* </div> */}
              </Grid>
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.recjected}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ recjected: false })}
          isBlocking={false}
        >
          <div className="statusCommentscancel">
            {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
            <IconButton
              // styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ recjected: false });
              }}
            />
          </div>
          {/* <div className={PIMcontentStyles.body}> */}
          <div className="statusComments">
            <form noValidate autoComplete="off">
              <TextField
                styles={rejectComment}
                className="textField"
                label={`${this.props.t('comment')}`}
                value={this.state.descriptionValue}
                name="comment"
                required
                resizable={false}
                multiline
                autoAdjustHeight
                data-attribute-name="Name"
                data-async
              />
            </form>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.imgVisible}
          containerClassName={PIMcontentStyles.imageContainerPhotoshote}
          onDismiss={() =>
            this.setState({
              imgVisible: false,
              RemovedimageId: [],
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext3">{t('IMAGE_UPLOAD')}</span>
          </div>
          <div className="statusCommentscancel">
            <IconButton
              styles={iconButtonStyles}
              className="Imagecancel"
              iconProps={cancelIcon}
              ariaLabel="Close image"
              onClick={() => {
                this.setState({
                  imgVisible: false,
                  image: [],
                  RemovedimageId: [],
                });
              }}
              style={{ marginRight: '1px' }}
            />
          </div>
          <div className={PIMcontentStyles.imagebody}>
            {!(this.state.imageLoading === true && this.state.imageURL.length === 0) ? (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '-5px' }}>
                {this.state.imageURL.map((imageUrl: any, index: any) => (
                  <div
                    key={index}
                    style={{
                      boxSizing: 'border-box',
                      width: 'calc(33.33% - 5px)',
                      marginBottom: '10px',
                      position: 'relative',
                    }}
                  >
                    <>
                      <Image
                        src={imageUrl}
                        alt={`picture-${index}`}
                        style={{
                          width: '93%',
                          height: '130px',
                          objectFit: 'cover',
                          cursor: 'pointer',
                        }}
                        onClick={() =>
                          this.setState({
                            fullImageVisible: true,
                            fullImageSrc: imageUrl,
                          })
                        }
                      />
                    </>
                  </div>
                ))}
              </div>
            ) : (
              <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
            )}
          </div>
          <Grid lg={12} item container spacing={2}>
            <Grid item lg={4.2} xs={12}></Grid>
            <Grid item lg={3} xs={12}>
              <DefaultButton
                className="button"
                styles={PIMfooterButtonConfirm}
                text={`${this.props.t('BTN_CANCEL')}`}
                onClick={() => {
                  this.setState({ imgVisible: false, image: [], RemovedimageId: [] });
                }}
              />
            </Grid>
          </Grid>
        </Modal>
        <Modal
          className="fullImageModal"
          isOpen={this.state.fullImageVisible}
          containerClassName={PIMcontentStyles.fullImageContainer}
          isBlocking={false}
          styles={{
            main: {
              width: '600px',
              height: '400px',
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
            },
          }}
          onDismiss={() =>
            this.setState({
              fullImageVisible: false,
              fullImageSrc: '',
            })
          }
        >
          <div
            style={{
              //   position: 'relative',
              // width: '100%',
              // height: '100%',
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
            }}
          >
            {/* <span className="apptext1">{t('IMAGE')}</span> */}
            <IconButton
              styles={iconButtonStyles1}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.closingFullImageModal();
              }}
            />
            <Image
              src={this.state.fullImageSrc}
              alt={`picture`}
              style={{
                maxWidth: '100%',
                maxHeight: '100%',
                objectFit: 'contain',
                display: 'block',
                marginLeft: 'auto',
                marginRight: 'auto',
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
              }}
            />
            <IconButton
              styles={iconButtonStyles123}
              iconProps={ChevronLeft}
              ariaLabel="Previous Picture"
              onClick={this.showPreviousImage}
              style={{
                position: 'absolute',
                left: '10px',
                top: '50%',
                transform: 'translateY(-50%)',
              }}
            />
            <IconButton
              styles={iconButtonStyles12}
              iconProps={ChevronRight}
              ariaLabel="Next Picture"
              onClick={this.showNextImage}
              style={{
                position: 'absolute',
                right: '10px',
                top: '50%',
                transform: 'translateY(-50%)',
              }}
            />
          </div>
        </Modal>

        {this.state.showActionColumn == false && this.rejected == false && (
          <div className="dataTableFixedFooter">
            <DataTable
              columns={_columns}
              data={this.state.filteredRecord}
              pagination={true}
              // selectableRowsHighlight={this.state.selectableRowsHighlight == false ? false : true}
              // selectableRows={this.state.selectedRowData}
              // selectableRowsVisibleOnly={this.state.selectedRowData}
              // clearSelectedRows={this.state.noSelection}
              onSelectedRowsChange={this.rowSelection}
              onColumnOrderChange={handleColumnReorder}
              highlightOnHover
              fixedHeader
              fixedHeaderScrollHeight="58vh"
              striped
              paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
              paginationPerPage={this.Defaultperpage()}
              paginationRowsPerPageOptions={this.perpage()}
              noDataComponent={
                <div className="noDataWidth">
                  {<DataTable columns={_columns} data={[{ '': '' }]} />}
                  <Stack className="noRecordsWidth">
                    {this.state.isRecord == true
                      ? `${this.props.t('RECORDS')}`
                      : `${this.props.t('NO_RECORDS')}`}
                  </Stack>
                </div>
              }
            />
          </div>
        )}
        {this.rejected == true && this.state.showActionColumn == false && (
          <div className="dataTableFixedFooter">
            <DataTable
              columns={_columns_item}
              data={this.state.rejecteddata}
              pagination={true}
              // selectableRowsHighlight={this.state.selectableRowsHighlight == false ? false : true}
              // selectableRows={this.state.selectedRowData}
              // selectableRowsVisibleOnly={this.state.selectedRowData}
              // clearSelectedRows={this.state.noSelection}
              onSelectedRowsChange={this.rowSelection}
              onColumnOrderChange={handleColumnReorder1}
              highlightOnHover
              fixedHeader
              fixedHeaderScrollHeight="58vh"
              striped
              paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
              paginationPerPage={this.Defaultperpage()}
              paginationRowsPerPageOptions={this.perpage()}
              noDataComponent={
                <div className="noDataWidth">
                  {<DataTable columns={_columns_item} data={[{ '': '' }]} />}
                  <Stack className="noRecordsWidth">
                    {this.state.isRecord1 == true
                      ? `${this.props.t('RECORDS')}`
                      : `${this.props.t('NO_RECORDS')}`}
                  </Stack>
                </div>
              }
            />
          </div>
        )}
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(CRTPhotoShootTracking);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
