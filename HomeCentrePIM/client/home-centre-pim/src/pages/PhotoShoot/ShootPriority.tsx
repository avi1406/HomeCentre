import {
  DefaultButton,
  SearchBox,
  Stack,
  IconButton,
  Dropdown,
  Icon,
  ComboBox,
  Modal,
  TextField,
  Image,
  ContextualMenu,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { DocumentCheckmarkFilled } from '@fluentui/react-icons';
import { Grid } from '@mui/material';
import axios from 'axios';
import {
  columnHeader,
  customModalStyles,
  hyperlink,
  PIMButtons,
  PIMcontentStyles,
  PIMDropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import DataTable from 'react-data-table-component';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import {
  CancelStyles,
  UploadStyles,
  approveIcon,
  cancelIcon,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  iconButtonStyles1,
  iconButtonStyless,
  iconButtonStyless1,
  iconButtonStylesssClicked,
  iconButtonStylesssClicked1,
  rejectComment,
  rejectIcon,
} from '@/common/properties';
import { baseUrl } from '@/services/Constants';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import { CamQc, ImageTypeDropdown, ProductImageModel, UidSearchModel } from '@/services/ApiClient';
import UpdateItemCode from './UpdateItemCode';
import { FaRegClone } from 'react-icons/fa';
import AuthManagerService from '@/services/AuthManagerService';

class SHOOTPRIORITY extends React.Component<any, any> {
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token: any = sessionStorage.getItem('token');
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  username: any = this.user?.name || '';
  selectedSeason1: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      isRecord: true,
      searchKey: '',
      pictureData: [],
      selectedRowData: [],
      imageURL: '',
      fromDate: null,
      minDate: undefined,
      toDate: undefined,
      imgVisible: false,
      imageValueUrl: '',
      imageLoading: false,
      selectedSeason1: [],
      refreshKey: 0,
      exportvisible: false,
      selectedOption: '',
      isItDrape: true,
      isItPhysical: true,
      dropDownSelectedRecord: [],
      optionSelected: [],
      isACL2: false,
      isACL1: false,
      divisionSelected: 0,
      actionL1Selected: 0,
      photoShootOption: 0,
      A_D_Visible: false,
      IMdropdownOptions: [],
      selectedOptionIm: null,
      filteredRecord: [],
      showActionColumn: false,
      approveVisible: false,
      recjectVisible: false,
      QualitycheckIM: null,
      Qualitycheck: [],
      QualitycheckM: false,
      itemcode: null,
      QualitycheckIMVal: '',
      otherSplitComment: '',
      Picturedata: [],
      imageURLs: [] as string[],
      fullImageVisible: false,
      fullImageSrc: '',
    };
  }
  division: any = [];
  actionL1: any = [];
  actionL2: any = [];
  Imageoption: any = 'All';
  commonDateRef = React.createRef<CommonDateRef>();

  async componentDidMount() {
    this.refresh(this.state.fromDate, this.state.toDate);
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
      deptOpt.push({ key: element.id, text: element.name });
    });
    var handOverList = (await this.apiClient.getHandOverStatus())?.result;
    var handOverOption: { key: number | undefined; text: string | undefined }[] = [];
    var hanDOverOpt: { key: number | undefined; text: string | undefined }[] = [];
    handOverList?.forEach((element) => {
      handOverOption.push({ key: element.id, text: element.name });
      hanDOverOpt.push({ key: element.id, text: element.name });
    });
    var photoShootList = (await this.apiClient.getPhotoShootStatus())?.result;
    var photoShootOption: { key: number | undefined; text: string | undefined }[] = [];
    var photoshoOpt: { key: number | undefined; text: string | undefined }[] = [];
    photoShootList?.forEach((element) => {
      photoShootOption.push({ key: element.id, text: element.name });
      photoshoOpt.push({ key: element.id, text: element.name });
    });

    const rejectComment = (await this.apiClient.getRejectReason()).result;
    var comment: any = [];
    rejectComment?.forEach((element) => {
      comment.push({ key: element.name, text: element.name });
    });
    this.setState({
      departmentOption: departmentOption,
      rejectCommentList: comment,
      handOverOption: handOverOption,
      photoShootOption: photoshoOpt,
    });
    this.setState({ selectedOption: 'all' });
    const response: any = (await this.apiClient.getItemCodeNumbers()).result;
    var locationoption: any = [];
    response?.forEach((element: { city: any }) => {
      locationoption.push({ key: element.city, text: element.city });
    });
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const QualityCheck = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'QualityCheck')?.jsonValue,
    );
    var nameoption: any = [];
    response?.forEach((element: { name: any }) => {
      nameoption.push({ key: element.name, text: element.name });
    });
    var pirorityoption: any = [];
    response?.forEach((element: { priority: any }) => {
      pirorityoption.push({ key: element.priority, text: element.priority });
    });
    this.setState({
      cityoption: locationoption,
      spirorityoption: pirorityoption,
      Qualitycheck: QualityCheck,
      snameoption: nameoption,
    });
  }
  // dragOptions: IDragOptions = {
  //   moveMenuItemText: 'Move',
  //   closeMenuItemText: 'Close',
  //   menu: ContextualMenu,
  //   dragHandleSelector: '.ms-Modal-scrollableContent > div:first-child',
  // };
  // async refresh(startDate: any, endDate: any) {
  //   try {
  //     this.setState({
  //       isRecord: true,
  //     });
  //     var Picture: any = (await this.apiClient.getPictureCamQcL1())?.result;
  //     var studio: any = (await this.apiClient.getstudio()).result;
  //     var studioDetails: any = (await this.apiClient.getStudios()).result;
  //     const filteredStudios = studio.filter((studio: any) => {
  //       const today = new Date();
  //       const formattedToday = {
  //         receivedDate: new Date(today).toLocaleDateString(),
  //       };
  //       const checkdatenull = studio.effectivefromDate === null ? false : true;
  //       const checkdate = studio.effectivefromDate === '0001-01-01T00:00:00' ? false : true;
  //       const effectiveFromDate =
  //         studio.effectivefromDate === '0001-01-01T00:00:00'
  //           ? formattedToday.receivedDate
  //           : new Date(studio.effectivefromDate).toLocaleDateString();
  //       const isEffectiveToday =
  //         new Date(effectiveFromDate) <= new Date(formattedToday.receivedDate);
  //       const isActive = studio.isActive;
  //       const isCapacityZero = formattedToday.receivedDate !== null;
  //       return isActive && isEffectiveToday && isCapacityZero && checkdate && checkdatenull;
  //     });
  //     this.setState({ searchKey: '', Picturedata: Picture });
  //     const fromDate = startDate?.toLocaleDateString('en-CA') || '';
  //     const toDate = endDate?.toLocaleDateString('en-CA') || '';
  //     const imageid = this.state.optionSelected;
  //     const response: any = (await this.apiClient.getCamQcdetails(fromDate, toDate, imageid))
  //       .result;

  //     var pictureoption: any = [];
  //     response?.forEach((element: { imageUrl: any }) => {
  //       pictureoption.push({ key: element.imageUrl, text: element.imageUrl });
  //     });
  //     const filteredResponse = response?.filter((item: any) =>
  //       this.deptId.includes(item.divisionId),
  //     );
  //     const responeWithIndex = filteredResponse.map((item: any, index: any) => ({
  //       index: index + 1,
  //       ...item,
  //     }));

  //     let data: any[] = []; // Initialize data as an empty array

  //     if (studioDetails.length > 0) {
  //       responeWithIndex.forEach((element: any) => {
  //         const detail = studioDetails.find((e: any) => element.itemCode === e.itemCode);
  //         if (detail) {
  //           const newdata = {
  //             ...element,
  //             StudioName: detail.name,
  //             StudioPriority: detail.priority,
  //             StudioCapacity: detail.capacity,
  //             StudioCity: detail.city,
  //             StudioAllocationDate: detail.allocationDate,
  //           };
  //           data.push(newdata);
  //         } else {
  //           data.push(element);
  //         }
  //       });
  //     } else {
  //       data = responeWithIndex;
  //     }

  //     var ImageTypeList = (await this.apiClient.getProductImageType())?.result;
  //     var ImageTypeOpt: { key: number | undefined; text: string | undefined }[] = [];
  //     ImageTypeList?.forEach((element) => {
  //       if (element.id === 4) {
  //         ImageTypeOpt.unshift({ key: element.id, text: element.name });
  //       } else {
  //         ImageTypeOpt.push({ key: element.id, text: element.name });
  //       }
  //     });
  //     var defaultdata = ImageTypeOpt.find((e: any) => e.key == 4);
  //     this.setState({
  //       IMdropdownOptions: ImageTypeOpt,
  //       selectedOptionIm: defaultdata?.key,
  //     });
  //     this.setState({
  //       studio: filteredStudios,
  //       studiofilter: filteredStudios,
  //       pictureData: response,
  //       filteredRecord: data,
  //       currentRecord: data,
  //       dropDownSelectedRecord: data,
  //       isRecord: false,
  //       exportvisible: data.length !== 0 ? true : false,
  //       noSelection: !this.state.noSelection,
  //     });
  //   } catch (e: any) {
  //     this.notify.showErrorNotify(e.message);
  //   }
  //   this.Imageoption = 'All';
  //   this.actionL1 = [];
  //   this.actionL2 = [];
  //   this.selectedSeason1 = [];
  //   this.setState({
  //     selectedOption: 'all',
  //     isItPhysical: true,
  //     isItDrape: true,
  //     filteredRecord: this.state.currentRecord,
  //     searchKey: '',
  //     Division: '',
  //     ActionL2: '',
  //     ActionL1: '',
  //     division: [],
  //     actionL1: [],
  //     actionL2: [],
  //     divisionSelected: 0,
  //     isACL2: false,
  //     isDivision: false,
  //     isACL1: false,
  //     itemcode: [],
  //   });
  // }
  async refresh(startDate: any, endDate: any): Promise<void> {
    try {
      this.setState({ isRecord: true });
  
      const [pictureDataResponse, studioResponse, studioDetailsResponse, imageTypeListResponse] = await Promise.all([
        this.apiClient.getPictureCamQcL1(),
        this.apiClient.getstudio(),
        this.apiClient.getStudios(),
        this.apiClient.getProductImageType(),
      ]);
  
      const Picture = pictureDataResponse?.result || [];
      const studio = studioResponse?.result || [];
      const studioDetails = studioDetailsResponse?.result || [];
      const today = new Date().toLocaleDateString();
  
      const filteredStudios = studio.filter((studio: any) => {
        const effectiveFromDate = studio.effectivefromDate ? new Date(studio.effectivefromDate).toLocaleDateString() : today;
        const isEffectiveToday = new Date(effectiveFromDate) <= new Date(today);
        return studio.isActive && isEffectiveToday && studio.effectivefromDate !== null && studio.effectivefromDate !== '0001-01-01T00:00:00';
      });
  
      this.setState({ searchKey: '', Picturedata: Picture });
  
      const fromDate = startDate?.toLocaleDateString('en-CA') || '';
      const toDate = endDate?.toLocaleDateString('en-CA') || '';
      const imageid = this.state.optionSelected;
      const camQcdetailsResponse = await this.apiClient.getCamQcdetails(fromDate, toDate, imageid);
      const response = camQcdetailsResponse?.result || [];
  
      const filteredResponse = response.filter((item: any) => this.deptId.includes(item.divisionId));
      const responeWithIndex = filteredResponse.map((item: any, index: number) => ({
        index: index + 1,
        ...item,
      }));
  
      const data = responeWithIndex.map((element: any) => {
        const detail = studioDetails.find((e: any) => element.itemCode === e.itemCode);
        if (detail) {
          return {
            ...element,
            StudioName: detail.name,
            StudioPriority: detail.priority,
            StudioCapacity: detail.capacity,
            StudioCity: detail.city,
            StudioAllocationDate: detail.allocationDate,
          };
        }
        return element;
      });
  
      const ImageTypeList = imageTypeListResponse?.result || [];
      const ImageTypeOpt = ImageTypeList.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      const defaultdata = ImageTypeOpt.find((e: any) => e.key === 4);
      if (defaultdata) {
        ImageTypeOpt.unshift(ImageTypeOpt.splice(ImageTypeOpt.indexOf(defaultdata), 1)[0]);
      }
  
      this.setState({
        IMdropdownOptions: ImageTypeOpt,
        selectedOptionIm: defaultdata?.key,
        studio: filteredStudios,
        studiofilter: filteredStudios,
        pictureData: response,
        filteredRecord: data,
        currentRecord: data,
        dropDownSelectedRecord: data,
        isRecord: false,
        exportvisible: data.length > 0,
        noSelection: !this.state.noSelection,
        selectedOption: 'all',
        isItPhysical: true,
        isItDrape: true,
        searchKey: '',
        Division: '',
        ActionL2: '',
        ActionL1: '',
        division: [],
        actionL1: [],
        actionL2: [],
        divisionSelected: 0,
        isACL2: false,
        isDivision: false,
        isACL1: false,
        itemcode: [],
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
  
    this.Imageoption = 'All';
    this.actionL1 = [];
    this.actionL2 = [];
    this.selectedSeason1 = [];
  }  
  Imagedisplay = (pictureIds: any) => {
    const imageURLs: string[] = [];
    let imageLoading = false;

    if (pictureIds != null) {
      pictureIds.split(',').forEach((Id: any) => {
        const url = this.state.Picturedata?.find((e: any) => e.id == Id)?.imageUrl;
        const value = url?.split('?');
        const ImgUrl = value == undefined || null ? '' : value[0];
        if (ImgUrl !== '') {
          imageURLs.push(ImgUrl);
        } else {
          imageLoading = true;
        }
      });
      this.setState({
        imgVisible: true,
        imageURLs: imageURLs,
        imageLoading,
      });
    } else {
      this.setState({
        imgVisible: true,
        imageLoading: true,
      });
    }
  };
  closingImageModal = () => {
    this.setState({ imgVisible: false, imageURLs: [] });
  };
  closingFullImageModal = () => {
    this.setState({ fullImageVisible: false, fullImageSrc: '' });
  };
  Quality = async () => {
    this.setState({ QualitycheckM: false, QualitycheckIM: null });
    if (this.state.itemcode != null) {
      const Itemcode = parseInt(this.state.itemcode, 10);
      const check = 'Pass';
      await this.apiClient.updateCAMQCL1QualityCheck(Itemcode, check, this.username);
      this.notify.showSuccessNotify(this.props.t('MSG_APPROVED'));
      this.refresh(null, null);
    }
  };

  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }

  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date, downloadFromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date, downloadToDate: date });
  };

  dropFilter = async () => {
    var finalData: any = [];
    if (this.state.IMdropdownOptions.length != 0) {
      if (this.Imageoption != 'All') {
        finalData = this.state.currentRecord.filter((element: any) => {
          return element.imageType == this.Imageoption;
        });
      } else {
        finalData = this.state.currentRecord;
      }
      this.setState({
        filteredRecord: finalData,
      });
    } else {
      this.setState({
        filteredRecord: this.state.currentRecord,
      });
    }
  };
  columnFilter = async () => {
    var data: any = [];
    var secondaryFilter: any = [];
    var finalData: any = [];
    if (this.division.length != 0) {
      data = this.state.currentRecord.filter((element: any) => {
        return element.divisionId == this.division;
      });
    } else {
      data = this.state.currentRecord;
    }
    if (this.actionL1.length != 0) {
      secondaryFilter = data.filter((element: any) => {
        return element.handoverStatus == this.actionL1;
      });
    } else {
      secondaryFilter = data;
    }
    if (this.actionL2.length != 0) {
      finalData = secondaryFilter.filter((element: any) => {
        return element.photoShootStatus == this.actionL2;
      });
    } else {
      finalData = secondaryFilter;
    }
    this.setState({
      dropDownSelectedRecord: finalData,
      filteredRecord: finalData,
      exportvisible: finalData.length !== 0 ? true : false,
    });
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { dropDownSelectedRecord } = this.state;
    var searchResult = dropDownSelectedRecord.filter((element: any) => {
      return element?.itemCode.toString().includes(newValue.trim());
    });
    this.setState({
      // filteredRecord: searchResult.map((item: any, index: any) => ({ index: index + 1, ...item })),
      filteredRecord: searchResult,
      searchKey: newValue,
      exportvisible: searchResult.length !== 0,
    });
  }

  async onFilterClick() {
    var { toDate, minDate, searchDivision } = this.state;
    var body = new UidSearchModel();
    if (searchDivision != undefined) body.departmentId = searchDivision?.key;
    if (minDate != undefined) {
      const dateParts = new Date(minDate).toLocaleDateString('en-CA').split('/');
      body.fromDate = dateParts[0];
      minDate = body.fromDate;
    }
    if (toDate != undefined) {
      const dateParts = new Date(toDate).toLocaleDateString('en-CA').split('/');
      body.toDate = dateParts[0];
      toDate = body.toDate;
    }
    const response = (await this.apiClient.searchCamQcdetails(minDate, toDate)).result;
    const responeWithIndex = response?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    this.setState({
      searchKey: '',
      isDivision: false,
      divisionSelected: 0,
      isACL1: false,
      actionL1Selected: 0,
      actionL2Selected: 0,
      isACL2: false,
      onClickMinDate: minDate,
      onClickToDate: toDate,
      onClickSearchDivision: searchDivision,
      currentRecord: responeWithIndex,
      dropDownSelectedRecord: responeWithIndex,
      filteredRecord: responeWithIndex,
      exportvisible: responeWithIndex?.length !== 0 ? true : false,
    });
  }
  downloadFile = (minDate: any, toDate: any, imageType: any) => {
    var body = new ImageTypeDropdown();
    body.fromDate = this.state.downloadFromDate?.toLocaleDateString('en-CA');
    body.toDate = this.state.downloadToDate?.toLocaleDateString('en-CA');
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.join(',');
    body.userDivision = commaSeparatedString;
    switch (this.state.optionSelected) {
      case 1:
        body.imageType = 'Drape Image';
        break;
      case 2:
        body.imageType = 'Physical Sample';
        break;
      case 3:
        body.imageType = 'GRN';
        break;
      case 4:
        body.imageType = 'All';
        break;
      default:
        body.imageType = this.Imageoption;
        break;
    }
    // body.imageType = this.Imageoption;
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/CamQc/CAMQCL1ExportExcel`,
      method: 'POST',
      responseType: 'blob',
    })
      .then((response) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'ShootPlannerExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };

  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };

  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  handleRowClick = (row: any) => {
    const { studiofilter } = this.state;
    let otherStudiosWithCapacity;
    const sameNameStudios = studiofilter.filter((s: any) => {
      return s.name == row.StudioName;
    });
    if (row.divisionId == 4) {
      otherStudiosWithCapacity = studiofilter.filter((s: any) => {
        return (
          s.divisionId === row.divisionId && s.name !== row.StudioName && s.remainingCapacity !== 0
        );
      });
    } else if (row.divisionId == 61) {
      otherStudiosWithCapacity = studiofilter.filter((s: any) => {
        return (
          s.divisionId == row.divisionId && s.name != row.StudioName && s.remainingCapacity != 0
        );
      });
    } else if (row.divisionId == 65) {
      otherStudiosWithCapacity = studiofilter.filter((s: any) => {
        return (
          s.divisionId == row.divisionId && s.name != row.StudioName && s.remainingCapacity != 0
        );
      });
    } else {
      otherStudiosWithCapacity = studiofilter.filter((s: any) => {
        return s.name !== row.StudioName && s.remainingCapacity !== 0;
      });
    }
    const filteredStudios = [...sameNameStudios, ...otherStudiosWithCapacity];
    if (filteredStudios.length > 0) {
      this.setState(
        {
          studio: filteredStudios,
        },
        () => {
          this.setState({
            A_D_Visible: true,
            editRowdata: [row],
          });
        },
      );
    }
  };

  async onApproveBatch() {
    let data = true;
    this.setState({ approveVisible1: false });
    let { selectedRowData } = this.state;
    var itemCode: any[] = [];
    var city: any[] = [];
    var dates: any[] = [];
    selectedRowData.forEach((element: any) => {
      const recordid = element.city;
      city.push(recordid);
      const itemCodeid = element.itemCode;
      itemCode.push(itemCodeid);
      if (element.camqcstatus == 'Sent') {
        data = false;
        this.notify.showErrorNotify(this.props.t('ALL_READY_SUBMITTED'));
        this.setState({
          selectedRowData: [],
        });
        this.refresh(null, null);
        return;
      }
      if (element.l1Action == null) {
        data = false;
        this.notify.showErrorNotify(this.props.t('Go and Aknowledge First'));
        this.setState({
          selectedRowData: [],
        });
        this.refresh(null, null);
        return;
      }
      if (element.camqcl1qualitycheck != 'Pass' || element.camqcl1qualitycheck == null) {
        data = false;
        this.notify.showErrorNotify(this.props.t('PLEASE_DO_QUALITYCHECK'));
        this.setState({
          selectedRowData: [],
        });
        this.refresh(null, null);
        return;
      }
    });
    if (data == true) {
      const response = (await this.apiClient.updateAssigned(city, itemCode, dates, this.username))
        .result;
      if (response == null) {
        this.notify.showSuccessNotify(this.props.t('SUBMITTED_SUCCESSFULLY'));
      } else {
        this.setState({
          ApprovalEnable: true,
          ApproveSelectedClicked: false,
        });

        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      }
    }
    this.refresh(null, null);
    this.setState({
      selectedRowData: [],
    });
  }

  onApprove = async () => {
    try {
      this.setState({ approveVisible: false });
      var itemcode = this.state.row.itemCode;
      var status = 'Yes';
      const response = await this.apiClient.updateCAMQCL1Action(
        itemcode,
        status,
        undefined,
        this.username,
      );
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
      }
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };
  Action = async () => {
    let response: any;
    const rejectComment = this.state.otherSplitComment;
    if (this.state.approveVisible) {
      this.setState({ approveVisible: false });
      const Approve = 'Received';
      const Itemcode = parseInt(this.state.itemcode, 10);
      response = await this.apiClient.updateCAMQCL1Action(
        Itemcode,
        Approve,
        undefined,
        this.username,
      );
    } else if (this.state.QualitycheckMfail) {
      this.setState({ QualitycheckMfail: false });
      const reject = 'CamQCL1';
      const Itemcode = parseInt(this.state.itemcode, 10);
      response = await this.apiClient.rejectedItemcode(
        Itemcode,
        reject,
        rejectComment,
        this.username,
      );
    }
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
    } else {
      if (rejectComment == '') {
        this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
      } else {
        this.notify.showSuccessNotify(this.props.t('REJECTED_SUCESSFULLY'));
      }
    }
    this.refresh(null, null);
  };
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

  render() {
    const { t } = this.props;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
      },
      {
        key: 'itemcode',
        name: <div className={columnHeader}>{`${this.props.t('ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        isVisible: true,
        width: '140px',
        cell: (row: any) => (
          <span
            className={hyperlink}
            onClick={() => {
              if (row.camqcstatus != 'Sent') {
                if (row.l1Action == 'Received' && row.l1Action != null) {
                  this.handleRowClick(row);
                } else {
                  this.notify.showErrorNotify(`${this.props.t('ACKNWOLEDGE_BEFORE_EDIT_ERROR')}`);
                }
              } else {
                this.notify.showErrorNotify(`${this.props.t('CANNOT_EDIT_SUBMITTED_ITEMCODE')}`);
              }
            }}
          >
            {row.itemCode}
          </span>
        ),
      },
      {
        key: 'eancode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        width: '140px',
      },
      {
        key: 'action',
        name: <div className={columnHeader}>{`${this.props.t('COL_ACKNOWLEDGEMENT')}`}</div>,
        selector: (row: any) => {
          if (this.state.filteredRecord != null || this.state.filteredRecord != undefined) {
            return (
              <>
                {this.state.filteredRecord.length !== 0 && (
                  <Grid container direction={'row'} spacing={2}>
                    {(row.l1Action == 'Received' || row.l1Action == null) && (
                      <Grid item>
                        <IconButton
                          styles={
                            row.l1Action !== null
                              ? row.l1Action == 'Received'
                                ? iconButtonStylesssClicked
                                : iconButtonStyless
                              : iconButtonStylesssClicked1
                          }
                          iconProps={approveIcon}
                          //disabled={row.l1Action == 'Received'}
                          onClick={() => {
                            if (row.l1Action == null) {
                              this.setState({
                                itemcode: row.itemCode,
                                approveVisible: true,
                                recjectVisible: false,
                              });
                            }
                          }}
                        />
                      </Grid>
                    )}
                    {/* {(row.l1Action == 'no' || row.l1Action == null) && (
                      <Grid item>
                        <IconButton
                          styles={
                            row.l1Action !== null
                              ? row.l1Action == 'no'
                                ? iconButtonStyless1
                                : iconButtonStylesssClicked
                              : iconButtonStyless
                          }
                          iconProps={rejectIcon}
                          onClick={() => {
                            if (row.l1Action == null) {
                              this.setState({
                                itemcode: row.itemCode,
                                recjectVisible: true,
                                approveVisible: false,
                              });
                            }
                          }}
                        />
                      </Grid>
                    )} */}
                  </Grid>
                )}
              </>
            );
          }
          return null;
        },
        isVisible: true,
        minWidth: '165px',
      },
      {
        name: <div className={columnHeader}>{t('PRODUCT_QC_CHECK')}</div>,
        isVisible: true,
        width: '170px',
        selector: (row: any) => {
          if (this.state.filteredRecord != null || this.state.filteredRecord != undefined) {
            if (this.state.filteredRecord.length != 0) {
              return (
                <>
                  <Grid container direction={'row'} spacing={2}>
                    {/* <Grid item>
                    <IconButton
                      styles={
                        row.camqcl1qualitycheck != null
                          ? row.camqcl1qualitycheck == 'Pass'
                            ? iconButtonStylesssClicked
                            : row.camqcl1qualitycheck == 'Fail'
                            ? iconButtonStyless1
                            : iconButtonStylesssClicked1
                          : iconButtonStylesssClicked1
                      }
                      disabled={row.l1Action == null || row.l1Action == 'no'}
                      iconProps={approveIcon}
                      onClick={() => {
                        if (row.camqcl1qualitycheck == null) {
                          this.setState({ QualitycheckM: true, itemcode: row.itemCode });
                        }
                      }}
                    />
                  </Grid> */}
                    {(row.camqcl1qualitycheck == 'Pass' || row.camqcl1qualitycheck == null) && (
                      <Grid item>
                        <IconButton
                          // disabled={
                          //   (row.l1Action == null && row.camqcl1qualitycheck == 'Pass') ||
                          //   row.camqcl1qualitycheck == 'Pass'
                          // }
                          styles={
                            row.camqcl1qualitycheck !== null
                              ? row.camqcl1qualitycheck == 'Pass'
                                ? iconButtonStylesssClicked
                                : iconButtonStyless
                              : iconButtonStylesssClicked1
                          }
                          iconProps={approveIcon}
                          onClick={() => {
                            if (row.camqcl1qualitycheck == null) {
                              this.setState({
                                itemcode: row.itemCode,
                                QualitycheckM: true,
                                QualitycheckMfail: false,
                              });
                            }
                          }}
                          disabled={row.l1Action != 'Received' || row.l1Action == null}
                        />
                      </Grid>
                    )}
                    {(row.camqcl1qualitycheck == 'Fail' || row.camqcl1qualitycheck == null) && (
                      <Grid item>
                        <IconButton
                          // disabled={row.l1Action == null}
                          styles={
                            row.camqcl1qualitycheck !== null
                              ? row.camqcl1qualitycheck == 'Fail'
                                ? iconButtonStyless1
                                : iconButtonStylesssClicked
                              : iconButtonStyless
                          }
                          iconProps={rejectIcon}
                          onClick={() => {
                            if (row.camqcl1qualitycheck == null) {
                              this.setState({
                                itemcode: row.itemCode,
                                QualitycheckMfail: true,
                                QualitycheckM: false,
                              });
                            }
                          }}
                          disabled={row.l1Action != 'Received' || row.l1Action == null}
                        />
                      </Grid>
                    )}
                  </Grid>
                </>
              );
            }
          }
        },
      },
      // {
      //   key: 'PS_IMAGE',
      //   name: <div className={columnHeader}>{`${this.props.t('PS_IMAGE')}`}</div>,
      //   selector: (row: any) => {
      //     if (row.pictureid || row.itemCode) {
      //       return (
      //         <>
      //           <Icon
      //             iconName="PhotoCollection"
      //             title={t('IMAGE')}
      //             className="iconStyle"
      //             onClick={() => {
      //               this.Imagedisplay(row.pictureid);
      //             }}
      //           />
      //         </>
      //       );
      //     } else {
      //       return null;
      //     }
      //   },
      //   isVisible: true,
      //   Width: '130px',
      // },
      {
        key: 'LINK',
        name: <div className={columnHeader}>{this.props.t('IMAGE')}</div>,
        isVisible: true,
        width: '110px',
        selector: (row: any) => {
          if (row.imageUrl) {
            const isValidUrl = () => {
              try {
                new URL(row.imageUrl);
                return true;
              } catch (e) {
                return false;
              }
            };
            if (
              this.state.selectedOptionIm == 1 ||
              this.state.selectedOptionIm == 2 ||
              this.state.selectedOptionIm == 3 ||
              this.state.selectedOptionIm == 4
            ) {
              return (
                <>
                  <a href={isValidUrl() && row.imageUrl} target="_blank" rel="noopener noreferrer">
                    <Icon iconName="Link" title={row.imageUrl} className="linkIcon" />
                  </a>
                </>
              );
            }
          } else {
            return null;
          }
        },
      },
      {
        key: 'division',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9} style={{ marginTop: '12px' }}>
              <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.departmentOption}
                onChange={(e: any, selectedOption: any) => {
                  this.division = selectedOption.key;
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                selectedKey={this.state.divisionSelected}
                styles={columnFilter}
                style={{ marginLeft: -20, marginTop: '10px' }}
              />
              <IconButton
                style={{ marginLeft: -20, marginTop: '3%' }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isDivision && (
                <IconButton
                  style={{ marginLeft: 1, marginTop: '3%' }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  onClick={() => {
                    this.division = [];
                    this.setState({ isDivision: false, divisionSelected: 0 });
                    this.columnFilter();
                  }}
                  iconProps={clearFilter}
                />
              )}
            </Grid>
          </Grid>
        ),
        position: 'fixed',
        selector: (row: any) => {
          if (row.divisionId) {
            var division1 = null;
            const DivKey = parseInt(row.divisionId, 10);
            const statusObject = this.state.departmentOption
              ? this.state.departmentOption.find((element: any) => element.key === DivKey)
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
        key: 'date_received',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => <span title={row.receivedDate}>{row.receivedDate}</span>,
        isVisible: true,
        width: '120px',
      },
      {
        key: 'studio_name',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_NAME')}`}</div>,
        selector: (row: any) => <span title={row.StudioName}>{row.StudioName}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'studio_location',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_LOCATION')}`}</div>,
        selector: (row: any) => <span title={row.StudioCity}>{row.StudioCity}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'studio_capacity',
        name: <div className={columnHeader}>{`${this.props.t('CONTENT_STUDIO_CAPACITY')}`}</div>,
        selector: (row: any) => <span title={row.StudioCapacity}>{row.StudioCapacity}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      // {
      //   key: 'handover_status',
      //   name: (
      //     <Grid container direction={'row'} alignItems="center">
      //       <Grid item lg={8} xs={9} style={{ paddingRight: 10 }}>
      //         <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_HANDOVER_STATUS')}</div>
      //       </Grid>
      //       <Grid item lg={3} xs={3} style={{ display: 'flex', alignItems: 'center' }}>
      //         <ComboBox
      //           className="ShootPriorityDP"
      //           options={this.state.handOverOption}
      //           onChange={(e: any, selectedOption: any) => {
      //             this.actionL1 = selectedOption.text;
      //             this.setState({
      //               isACL1: true,
      //               actionL1Selected: selectedOption.text,
      //               searchKey: '',
      //             });
      //             this.columnFilter();
      //           }}
      //           styles={columnFilter}
      //           selectedKey={this.state.actionL1Selected}
      //           style={{ marginLeft: -30, marginTop: 10 }}
      //         />
      //         <IconButton
      //           style={{ marginLeft: 10 }}
      //           styles={filterButton}
      //           iconProps={filterIcon}
      //           title={'Filter'}
      //           ariaLabel="Close popup modal"
      //         />
      //         {this.state.isACL1 && (
      //           <IconButton
      //             style={{ marginLeft: 32, marginBottom: 10 }}
      //             title={'Clear Filter'}
      //             styles={filterButton}
      //             iconProps={clearFilter}
      //             onClick={() => {
      //               this.actionL1 = [];
      //               this.setState({ isACL1: false, actionL1Selected: 0 });
      //               this.columnFilter();
      //             }}
      //           />
      //         )}
      //       </Grid>
      //     </Grid>
      //   ),
      //   position: 'fixed',
      //   selector: (row: any) => <span title={row.handoverStatus}>{row.handoverStatus}</span>,
      //   isVisible: true,
      //   width: '190px',
      // },
      {
        key: 'Studio_Allocation_date',
        name: <div className={columnHeader}>{`${this.props.t('STUDIO_ALLOCATION_DATE')}`}</div>,
        selector: (row: any) => <span title={row.allocationDate}>{row.allocationDate}</span>,
        isVisible: true,
        width: '135px',
      },
      {
        key: 'type',
        name: <div className={columnHeader}>{`${this.props.t('TYPE')}`}</div>,
        selector: (row: any) => <span title={row.imageType}>{row.imageType}</span>,
        isVisible: true,
        width: '130px',
      },
      {
        key: 'priority',
        name: <div className={columnHeader}>{`${this.props.t('COL_PRIORITY')}`}</div>,
        selector: (row: any) => {
          if (this.state.filteredRecord != null || this.state.filteredRecord != undefined) {
            if (this.state.filteredRecordlength != 0) {
              if (row && row.length != 0) {
                if (row && row.StudioPriority) {
                  return <span title={row.StudioPriority}>{row.StudioPriority}</span>;
                } else {
                  if (row && row.imageType === 'GRN') {
                    return <span title={'P0'}>{'P0'}</span>;
                  } else if (
                    row.imageType === 'Drape Image' ||
                    row.imageType === 'Physical Sample'
                  ) {
                    return <span title={'P1'}>{'P1'}</span>;
                  }
                  <></>;
                }
              }
            } else {
              return null;
            }
          }
        },
        isVisible: true,
        width: '100px',
      },
      // {
      //   key: 'photoshoot_status',
      //   name: (
      //     <Grid container direction={'row'}>
      //       <Grid item lg={8} xs={9}>
      //         <div className={columnHeader}>
      //           {this.props.t('PLANOGRAM_TABLE_PHOTOSHOOT_STATUS')}
      //         </div>
      //       </Grid>
      //       <Grid item lg={2} xs={3}>
      //         <ComboBox
      //           className="dropdown"
      //           options={this.state.photoShootOption}
      //           onChange={(e: any, selectedOption: any) => {
      //             this.actionL2 = selectedOption.text;
      //             this.setState({
      //               isACL2: true,
      //               actionL2Selected: selectedOption.text,
      //               searchKey: '',
      //             });
      //             this.columnFilter();
      //           }}
      //           styles={columnFilter}
      //           selectedKey={this.state.actionL2Selected}
      //         // style={{ marginTop: 15 }}
      //         //style={{ marginLeft: -25 }}
      //         />
      //         <IconButton
      //           style={{ marginLeft: 1 }}
      //           styles={filterButton}
      //           iconProps={filterIcon}
      //           title={'Filter'}
      //           ariaLabel="Close popup modal"
      //         />
      //       </Grid>
      //       <Grid item lg={1} xs={1}>
      //         {this.state.isACL2 && (
      //           <IconButton
      //             style={{ marginLeft: 24 }}
      //             title={'Clear Filter'}
      //             styles={filterButton}
      //             iconProps={clearFilter}
      //             onClick={() => {
      //               this.actionL2 = [];
      //               this.setState({ isACL2: false, actionL2Selected: 0 });
      //               this.columnFilter();
      //             }}
      //           />
      //         )}
      //       </Grid>
      //     </Grid>
      //   ),
      //   position: 'fixed',
      //   selector: (row: any) => {
      //     switch (row.photoShootStatus) {
      //       case 'Pending':
      //         return (
      //           <span className={`customer-badge status-Pending`}>
      //             <span title={`${'Pending'}`}>{'Pending'}</span>
      //           </span>
      //         );
      //       case 'Completed':
      //         return (
      //           <span className={`customer-badge status-Approved`}>
      //             <span title={`${'Completed'}`}>{'Completed'}</span>
      //           </span>
      //         );
      //       default:
      //         return <span></span>;
      //     }
      //   },

      //   isVisible: true,
      //   minWidth: '150px',
      // },
      // {
      //   key: 'stock_return_to_qc',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
      //   isVisible: true,
      //   minWidth: '160px',
      //   selector: (row: any) => {
      //     switch (row.stockReturnToQC) {
      //       case '0':
      //         return <span title={`${'Yet To Return'}`}>{'Yet To Return'}</span>;
      //         break;
      //       case '1':
      //         return (
      //           <>
      //             <span title={`${'Returned'}`}>{'Returned'}</span>
      //             <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
      //               {row.comments != null ? (
      //                 <FaRegClone
      //                   onClick={() => {
      //                     this.setState({
      //                       recjected1: true,
      //                       descriptionValue1: row.comments,
      //                     });
      //                   }}
      //                 />
      //               ) : (
      //                 ''
      //               )}
      //             </span>
      //           </>
      //         );
      //         break;
      //       default:
      //         return <span></span>;
      //         break;
      //     }
      //   },
      // },
      (this.state.isItPhysical && {
        key: 'received_by_photoshoot',
        name: (
          <div className={columnHeader}>{`${this.props.t('PRODUCT_RECEIVED_BY_PHOTOSHOOT')}`}</div>
        ),
        selector: (row: any) => {
          switch (row.productreceived) {
            case 'Received':
              return (
                <span className={`customer-badge status-Approved`}>
                  <span title={`${'Received'}`}>{'Received'}</span>{' '}
                </span>
              );
              break;
            case 'Pending':
              return <span title={`${'Pending'}`}>{'Pending'}</span>;
              break;
            default:
              return <span></span>;
              break;
          }
        },
        isVisible: false,
        width: '130px',
      }) ||
        null,
    ]?.filter((item) => item.isVisible);
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    return (
      <div className="QClayout">
        <div className="QcModel1" />
        <Stack>
          <Grid container spacing={1} direction="row" alignItems="center" className="grid">
            <Grid item lg={10.6} xs={8}>
              {/* <Text
                  variant="xxLarge"
                  styles={PIMHearderText}
                  className="color-blue text"
                >{`${this.props.t('SHOOT_PRIORITY')}`}</Text> */}
            </Grid>
            <Grid item lg={1} xs={2} />
            <Grid item lg={0.4} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.filteredRecord.length !== 0) {
                    this.downloadFile(this.state.minDate, this.state.toDate, this.state.imageType);
                  }
                }}
              />
              <Grid item lg={0.9} xs={2} />
            </Grid>
          </Grid>
        </Stack>
        <Grid lg={12} item container spacing={0.8} direction={'row'}>
          <Grid item lg={0.1} xs={0.2} />
          <Grid item lg={1.92} xs={12}>
            <SearchBox
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ITEMCODE')}`}
              value={this.state.searchKey}
              onChange={(e: any) => this.onSearch(e)}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={1.7} xs={6}>
            <Dropdown
              options={this.state.IMdropdownOptions}
              styles={PIMDropdownStyles}
              key={this.state.refreshKey}
              placeholder={`${t('DROPDOWN_IMAGE_TYPE')}`}
              // defaultSelectedKey={4}
              selectedKey={this.state.optionSelected}
              onChanged={(option) => {
                this.setState({ selectedOptionIm: option.key, optionSelected: option.key });
                this.Imageoption = option.text;
                // this.dropFilter();
              }}
            />
          </Grid>
          <Grid item lg={2} xs={6}>
            <CommonDate
              onMinDateChange={(e: any) => this.handleMinDateChange(e)}
              onToDateChange={(e: any) => this.handleToDateChange(e)}
              ref={this.commonDateRef}
              label={`${t('RECEIVED_DATE')}`}
            />
          </Grid>{' '}
          <Grid item lg={0.4} xs={5.3}>
            {/* <Icon
              iconName="WaitlistConfirm"
              title={this.props.t('ADD_BTN_SUBMIT')}
              className="iconStyle"
              onClick={() => {
                if (this.state.selectedRowData != '') {
                  this.setState({ showActionColumn: true, approveVisible1: true });
                }
              }}
            /> */}
            <span title={this.props.t('ADD_BTN_SUBMIT')}>
              <DocumentCheckmarkFilled
                className="submitIconStyle"
                onClick={() => {
                  if (this.state.selectedRowData != '') {
                    this.setState({ showActionColumn: true, approveVisible1: true });
                  }
                }}
              />
            </span>
          </Grid>
          <Grid item lg={4.48} xs={6} />
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Search"
              title={this.props.t('BTN_SEARCH')}
              className="iconStyle"
              onClick={() => {
                if (
                  !(
                    !(this.state.toDate == undefined) ||
                    !(this.state.minDate == undefined) ||
                    !(this.state.searchDivision == null)
                  )
                    ? false
                    : true
                ) {
                  this.refresh(this.state.minDate, this.state.toDate);
                } else {
                  this.dropFilter();
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
                  this.state.fromDate ||
                  this.state.minDate ||
                  this.state.searchKey !== '' ||
                  this.state.optionSelected.length != 0
                ) {
                  this.setState({
                    fromDate: null,
                    minDate: null,
                    optionSelected: [],
                    searchKey: '',
                  });
                  this.refresh(null, null);
                  this.ClearDates();
                }
                this.setState({
                  // Imageoption: 'All',
                  // selectedOptionIm: '',
                });
                this.Imageoption = 'All';
              }}
            />
          </Grid>
          <Grid item lg={0.3} xs={1.6}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={async () => {
                this.setState({ isRecord: true, optionSelected: [] });
                await this.refresh(null, null);
                this.setState({ isRecord: false, downloadFromDate: null, downloadToDate: null });
                this.ClearDates();
                this.Imageoption = 'All';
              }}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.approveVisible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          //dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              approveVisible: false,
            })
          }
        >
        <div className={PIMcontentStyles.confirmHeaderUser1}>
          <IconButton
            styles={UploadStyles}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="Pstext">{`${this.props.t(
              'PI_TABLE_MODEL_ACKNOWLEDGE',
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
                  onClick={() => this.Action()}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ approveVisible: false });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.discriptionVisible}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ discriptionVisible: false })}
          isBlocking={false}
        >
          <div className={`${PIMcontentStyles.header} headeralign`}>
            <div className="apptext3"> {this.props.t('MODAL_LINK')} </div>
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ discriptionVisible: false });
              }}
            />
          </div>
          <div className={PIMcontentStyles.body}>
            <TextField
              value={this.state.imageValueUrl}
              className="textField"
              multiline
              autoAdjustHeight={true}
              borderless
              resizable={false}
              readOnly={true}
            />
          </div>
        </Modal>
        <Modal
          isOpen={this.state.QualitycheckMfail}
          containerClassName={PIMcontentStyles.container5}
          isBlocking={false}
          //dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              otherSplitComment: '',
              QualitycheckMfail: false,
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ QualitycheckMfail: false, row: [] })}
          />
          <div className={PIMcontentStyles.header}>
            <Text variant="xLarge" className="apptext3 txtps">{`${this.props.t(
              'QUALITY_CHECK_FAIL_CATLOG',
            )}`}</Text>
          </div>
          <div className={PIMcontentStyles.rejectPopup}>
            <form noValidate autoComplete="off">
              <TextField
                required
                name="otherComment"
                className="textField"
                label={this.props.t('Reject Comment')}
                onChange={(event: any) =>
                  this.setState({
                    otherSplitComment: event.target.value.trim().replace(/\s{2,}/g, ' '),
                  })
                }
                multiline
                resizable={false}
                maxLength={500}
                autoAdjustHeight={true}
              />
            </form>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => {
                    this.Action();
                  }}
                  disabled={this.state.otherSplitComment.trim() == ''}
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
                      QualitycheckMfail: false,
                      otherSplitComment: '',
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.QualitycheckM}
          containerClassName={PIMcontentStyles.confirmContaineruser2}
          isBlocking={false}
          //dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              QualitycheckM: false,
            })
          }
          // styles={customModalStyles}
        >
         <div className={PIMcontentStyles.confirmHeaderUser2}>
          <IconButton
           styles={UploadStyles}
           iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ QualitycheckM: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody7}>
            <Text variant="xLarge" className="Pstext">{`${this.props.t(
              'QUALITY_CHECK_PASS_CATLOG',
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
                  onClick={() => this.Quality()}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ QualitycheckM: false });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.imgVisible}
          containerClassName={PIMcontentStyles.imageContainerPhotoshote}
          // isBlocking={true}
          onDismiss={() =>
            this.setState({
              imgVisible: false,
              imageURLs: [],
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext1">{t('IMAGE_UPLOAD')}</span>
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.closingImageModal();
              }}
            />
          </div>
          <div className={PIMcontentStyles.imagebody}>
            {/* {this.state.imageLoading == false ? (
                <Image
                  src={this.state.imageURL}
                  alt="picture"
                  style={{ maxWidth: '500px', maxHeight: '400px', width: '100%', height: '100%' }}
                />
              ) : (
                <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
              )} */}
            {this.state.imageLoading === false ? (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '-5px' }}>
                {this.state.imageURLs.map((imageUrl: any, index: any) => (
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
              // this.state.imageURLs.map((url: any, index: any) => (
              //   <div key={index} style={{ marginTop: '15px' }}>
              //     <Image
              //       src={url}
              //       alt={`picture-${index}`}
              //       style={{
              //         maxWidth: '500px',
              //         maxHeight: '400px',
              //         width: '100%',
              //         height: '100%',
              //       }}
              //     />
              //   </div>
              // ))
              <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
            )}
          </div>
        </Modal>
        <Modal
          className="fullImageModal"
          isOpen={this.state.fullImageVisible}
          containerClassName={PIMcontentStyles.fullImageContainer}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              fullImageVisible: false,
              fullImageSrc: '',
            })
          }
        >
          <div>
            {/* <span className="apptext1">{t('IMAGE_UPLOAD')}</span> */}
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
                maxWidth: '900px',
                maxHeight: '400px',
                width: '100%',
                height: '100%',
              }}
            />
          </div>
        </Modal>
        <Modal
          isOpen={this.state.approveVisible1}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              approveVisible1: false,
              row: [],
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
            <IconButton
              styles={UploadStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ approveVisible1: false, row: [] });
              }}
            />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="apptext">{`${this.props.t(
              'PLANOGRAM_TABLE_MODEL_APPROVE',
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
                  onClick={() => {
                    this.setState({ approveVisible1: true });
                    this.onApproveBatch();
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ approveVisible1: false, row: [] });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>

        <Modal
          isOpen={this.state.A_D_Visible}
          containerClassName={PIMcontentStyles.attributeContainer}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              A_D_Visible: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeader}>
            <IconButton
              styles={CancelStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({
                  A_D_Visible: false,
                });
              }}
            />
          </div>
          <div className={PIMcontentStyles.header}>
            <Grid container spacing={2}>
              <Grid item xs={10.5}>
                <p>
                  <span className="apptext1">{`${t('COL_ITEM_CODE_NUMBER')}`}</span>
                </p>
              </Grid>
            </Grid>
          </div>
          <div className={PIMcontentStyles.body}>
            <UpdateItemCode
              ClosePopup={() => {
                this.setState({
                  A_D_Visible: false,
                });
              }}
              locationoption={this.state.cityoption}
              nameoption={this.state.snameoption}
              pirorityoption={this.state.spirorityoption}
              fields={this.state.editRowdata}
              studio={this.state.studio}
              dataRefresh={() => this.refresh(null, null)}
            />
          </div>
        </Modal>

        {/* <Modal
            isOpen={this.state.QualitycheckM}
            containerClassName={PIMcontentStyles.container1}
            isBlocking={false}
            dragOptions={this.dragOptions}
            onDismiss={() =>
              this.setState({
                QualitycheckM: false,
                QualitycheckIM: null,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <Text variant="xLarge" className="apptext3">{`${this.props.t(
                'Quality Check',
              )}`}</Text>
            </div>

            <div className={PIMcontentStyles.body}>
              <form noValidate autoComplete="off">
                <Grid lg={12} item container spacing={2} direction="row">
                  <Grid item lg={20} xs={12} />
                  <Grid item lg={2} xs={12} />
                  <Grid className="Catalogue" item lg={2} xs={12}>
                    {`${this.props.t('Catalogue Check')}`}
                  </Grid>
                  <Grid item lg={1.5} xs={12} />
                  <Grid item lg={5} xs={12}>
                    <Dropdown
                      placeholder={t('DROPDOWN_PLACEHOLDER')}
                      options={this.state.Qualitycheck}
                      styles={PIMDropdownStyles}
                      key={this.state.refreshKey}
                      selectedKey={this.state.QualitycheckIM}
                      onChanged={(option) => {
                        this.setState({
                          QualitycheckIM: option.key,
                          QualitycheckIMVal: option.text,
                        });
                      }}
                    />
                  </Grid>
                </Grid>
              </form>
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2} direction="row">
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('BTN_CONFIRM')}`}
                    onClick={() => {
                      this.Quality();
                    }}
                    disabled={this.state.QualitycheckIM == null}
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
                        QualitycheckM: false,
                        QualitycheckIM: null,
                      });
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal> */}
        <Modal
          isOpen={this.state.recjected}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ recjected: false })}
          isBlocking={false}
        >
          <div className="statusCommentscancel">
            {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ recjected: false });
              }}
            />
          </div>
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
        <div className="QCTable1 dataTableFixedFooter">
          <DataTable
            className="datatable"
            columns={columns}
            data={this.state.filteredRecord}
            onColumnOrderChange={handleColumnReorder}
            selectableRows={this.state.selectedRowData}
            clearSelectedRows={this.state.noSelection}
            selectableRowsVisibleOnly={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            pagination={true}
            selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeader
            striped
            fixedHeaderScrollHeight="62.8vh"
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
    );
  }
}
const ComponentTranslated = withTranslation()(SHOOTPRIORITY);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
