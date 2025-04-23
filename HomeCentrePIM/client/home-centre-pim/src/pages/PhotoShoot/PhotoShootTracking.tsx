import {
  ComboBox,
  ContextualMenu,
  DatePicker,
  DefaultButton,
  Dropdown,
  Image,
  IDragOptions,
  Icon,
  IconButton,
  Modal,
  SearchBox,
  Stack,
  TextField,
} from '@fluentui/react';
import { Grid } from '@mui/material';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { DocumentCheckmarkFilled } from '@fluentui/react-icons';
import {
  columnHeader,
  iconButtonStylesImg,
  PIMButtons,
  PIMcontentStyles,
  PIMDropdownStyles,
  PIMDropdownStyles3,
  PIMDropdownStyles4,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import AuthManagerService from '@/services/AuthManagerService';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
//import { iconButtonStyles } from '@/common/Button';
import {
  UploadimageStyles,
  approveIcon,
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
  iconButtonStyless,
  iconButtonStyless1,
  iconButtonStylessClicked,
  iconButtonStylesss,
  iconButtonStylesssClicked,
  iconButtonStylesssClicked1,
  rejectComment,
  rejectIcon,
  rejectReasonDropdown,
  iconButtonStyles123,
  UploadStyles,
} from '@/common/properties';
import {
  ImageTypeDropdown,
  ProductImageModel,
  UidSearchModel,
  UploadPicture,
} from '@/services/ApiClient';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import DataTable from 'react-data-table-component';
import { BlockBlobClient } from '@azure/storage-blob';
import UploadPictureFolder from '../Input/UploadPictureFolder';
import { baseUrl } from '@/services/Constants';
import axios from 'axios';
import { FaRegClone } from 'react-icons/fa';
import PsUploadImageFolder from '../Input/PsuploadPictureFolder';

class PhotoShootTracking extends React.Component<any, any> {
  constructor(props: any) {
    super(props);
    this.state = {
      searchKey: '',
      Search: [],
      isDivision: false,
      currentRecord: [],
      isRecord: false,
      selectedRowData: [],
      image: [],
      PSTracking: [],
      saveData: false,
      selectedOption: '',
      otherSplitComment: '',
      otherSplitComment1: '',
      StockReturnToqc: [],
      editedstockReturnToQC: {},
      RangeShotDrop: [],
      IMdropdownOptions: [],
      optionSelected: [],
      RemovedimageId: [],
      pictureData: [],
      ImageFolder: false,
      defaultdata: null,
      individualStyle: false,
      catologShot: false,
      rangeShotopt: null,
      noSelection: false,
      PSstatus: [],
      itemcode: null,
      validatedData: [],
      submit: false,
      submitstock: false,
      submitstockmodel: false,
      Submit1: true,
      selectedOptionIm: null,
      DatePicker: null,
      Imagedate: false,
      Imsubmit: false,
      Stucksubmit: false,
      returnopt: null,
      returnopt1: {},
      rangeenable: false,
      PSstatusopt: '',
      stockqc : '',
      Stucksubmit1: false,
      stockerror: false,
      fullImageVisible: false,
      fullImageSrc: '',
      imageURL: [],
      imageFileAndUrls: [],
      AllImageUrl: [],
      AllUrl: [],
      divisionSelected: null,
      Browseenable: false,
      droppedFiles: [],
    };
    this.prevImage = [];
  }
  dragOptions: IDragOptions = {
    moveMenuItemText: 'Move',
    closeMenuItemText: 'Close',
    menu: ContextualMenu,
    dragHandleSelector: '.ms-Modal-scrollableContent > div:first-child',
  };
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  userId: any = this.user?.id || 0;
  roleId: any[] = this.user?.roleId || [];
  username: any = this.user?.name || '';
  commonDateRef = React.createRef<CommonDateRef>();
  dropdownOptions1: any = true;
  Imageoption: any = 'All';
  PTstatus: any = [];
  SRstatus: any = [];
  DivisitonValue: any = [];
  prevImage: any = [];
  UserStudio: any = [];
  async componentDidMount(): Promise<void> {
    this.setState({
      isRecord: true,
    });
    const StudioData = (await this.apiClient.getUserStudioDetails(this.userId)).result;
    this.UserStudio = StudioData?.map((item: any) => item.studioId);
    var departmentList = (await this.apiClient.getDivision())?.result;
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      deptOpt.push({ key: element.id, text: element.name });
    });
    this.setState({
      deptOption: deptOpt,
    });
    this.refresh(null, null, null);
  }

  async refresh(startDate: any, endDate: any, optionSelected: any) {
    const picData = (await this.apiClient.getPSFileNameAndUrl()).result;
    this.setState({ isRecord: true, image: [], RemovedimageId: [] }); // PSTracking: []
    this.Imageoption = 'All';
    this.prevImage = [];
    try {
      var ImageTypeList = (await this.apiClient.getProductImageType())?.result;
      var ImageTypeOpt: { key: number | undefined; text: string | undefined }[] = [];
      ImageTypeList?.forEach((element) => {
        if (element.id === 4) {
          ImageTypeOpt.unshift({ key: element.id, text: element.name });
        } else {
          ImageTypeOpt.push({ key: element.id, text: element.name });
        }
      });
      var Photoshootstatus = (await this.apiClient.photoshootstatus())?.result;
      var PhotoSstatus: { key: number | undefined; text: string | undefined }[] = [];
      Photoshootstatus?.forEach((element) => {
        PhotoSstatus.push({ key: element.id, text: element.name });
      });
      var defaultdata = ImageTypeOpt.find((e: any) => e.key == 4);
      this.setState({
        IMdropdownOptions: ImageTypeOpt,
        selectedOptionIm: defaultdata?.key,
        PSstatus: PhotoSstatus,
        pictureData: picData,
      });
      const fromDate = startDate?.toLocaleDateString('en-CA') || '';
      const toDate = endDate?.toLocaleDateString('en-CA') || '';
      const imageid = this.state.optionSelected;
      this.setState({ searchKey: '' });
      const response: any = (await this.apiClient.getPhotoShoot(fromDate, toDate, imageid)).result;
      this.setState({
        searchData: response?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      });
      const filteredResponse = response?.filter((item: any) =>
        this.deptId.includes(item.divisionId),
      );
      const responeIndex = filteredResponse.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      const responeWithIndex =
        responeIndex && this.roleId.includes(16)
          ? responeIndex.filter((item: any) => this.UserStudio.includes(item.studioId))
          : responeIndex;
      const appConfiguration: any = await this.apiClient.getAppConfig();
      const StockReturnToqc = JSON.parse(
        appConfiguration?.result?.find((e: any) => e.name == 'StockReturnToqc')?.jsonValue,
      );
      const RangeShotDrop = JSON.parse(
        appConfiguration?.result?.find((e: any) => e.name == 'RangeShot')?.jsonValue,
      );
      // const picData = (await this.apiClient.getFileNameAndUrl()).result;
      this.setState({
        PSTracking: responeWithIndex,
        Search: responeWithIndex,
        // pictureData: picData,
        currentRecord: responeWithIndex,
        isRecord: true,
        //isRecord: this.state.PSTracking.length > 0 ? true : false,
        exportvisible: responeWithIndex.length !== 0 ? true : false,
        noSelection: !this.state.noSelection,
        StockReturnToqc: StockReturnToqc,
        RangeShotDrop: RangeShotDrop,
        itemcode: null,
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.PTstatus = [];
    this.SRstatus = [];
    this.DivisitonValue = [];
    this.setState({
      isDivision: false,
      exportvisible: this.state.currentRecord.length !== 0 ? true : false,
      dropDownSelectedRecord: this.state.currentRecord,
      filteredRecord: this.state.currentRecord,
      selectedRowData: [],
      searchKey: '',
      isRecord: false,
      PSstatusopt: '',
      stockqc: '',
      divisionSelected: null,
    });
  }
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }

  downloadFile = (fromDate: any, toDate: any, imageType: any) => {
    var body = new ImageTypeDropdown();
    body.fromDate = this.state.fromDate?.toLocaleDateString('en-CA');
    body.toDate = this.state.toDate?.toLocaleDateString('en-CA');
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.map((id) => `'${id}'`).join(',');
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

    //body.imageType = this.Imageoption;

    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/PhotoShoot/PhotoShootExportExcel`,
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
        link.setAttribute('download', 'ShootManagementExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };

  setImages(folder: any) {
    var SetImage1: File[] = [];
    SetImage1 = folder;
  }

  // columnFilter = async () => {
  //   let finalData = this.state.currentRecord;

  //   if (this.state.IMdropdownOptions.length !== 0 && this.state.selectedOptionIm !== 4) {
  //     finalData = finalData.filter((element: any) => element.imagetype === this.Imageoption);
  //   }
  //   if (this.PTstatus.length !== 0) {
  //     if (this.PTstatus === 'Pending' || this.PTstatus === '1') {
  //       finalData = finalData.filter((element: any) => element.photoShootStatus === '1');
  //     } else {
  //       finalData = finalData.filter((element: any) => element.photoShootStatus === '2');
  //     }
  //   }
  //   this.setState({
  //     dropDownSelectedRecord: finalData,
  //     PSTracking: finalData,
  //   });
  // };
  // columnFilter1 = async () => {
  //   let finalData = this.state.currentRecord;
  //   if (this.DivisitonValue.length != 0) {
  //     finalData = this.state.searchData.filter((element: any) => {
  //       return element.divisionId == this.DivisitonValue;
  //     });
  //     this.setState({
  //       filteredRecord:finalData,
  //       PSTracking: finalData,
  //     });
  //   } else {
  //     this.setState({
  //       PSTracking: finalData,
  //     });
  //   }
  // };

  columnFilter = async () => {
    let finalData = this.state.currentRecord;

    if (this.state.IMdropdownOptions.length !== 0 && this.state.selectedOptionIm !== 4) {
      finalData = finalData.filter((element: any) => element.imagetype === this.Imageoption);
    }

    if (this.PTstatus.length !== 0) {
      if (this.PTstatus === 'Pending' || this.PTstatus === '1') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === '1');
      } else if (this.PTstatus === 'Completed' || this.PTstatus === '2') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === '2');
      } else if (this.PTstatus === 'Closed' || this.PTstatus === '3') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === '3');
      }
    }

    if (this.DivisitonValue.length !== 0) {
      finalData = finalData.filter((element: any) => element.divisionId == this.DivisitonValue);
    }

    // if (this.SRstatus) {
    //   if (this.SRstatus == '0') {
    //     finalData = finalData.filter((element: any) => element.stockReturnToQC === null || element.stockReturnToQC === '0');
    //   } else if (this.SRstatus == '1') {
    //     finalData = finalData.filter((element: any) => element.stockReturnToQC !== null && element.stockReturnToQC === '1');
    //   }
    // }
    
    if (this.SRstatus) {
      if (this.SRstatus === '1') { 
        finalData = finalData.filter((element: any) => element.stockReturnToQC === null || element.stockReturnToQC === '0');
      } else if (this.SRstatus === '2') {
        finalData = finalData.filter((element: any) => element.stockReturnToQC === '1');
      }
    }

    this.setState({
      PSTracking: finalData,
    });
  };

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: [] }, () => {
      this.setState({ selectedRowData: e.selectedRows });
    });
  };
  UploadPicture = async () => {
    try {
      var image = this.prevImage;
      if (image.length != 0) {
        for (var i = 0; i < image.length; i++) {
          const url = await this.apiClient.getAuthorizedUrlForWrite(image[i]?.name || '');
          if (!url.hasError) {
            this.setState({
              imageProgress: true,
              Imagetext: `uploading ${i + 1} of ${image.length}, ${image[i].name}`,
            });
            const Itemcode = parseInt(this.state.itemcode, 10);
            const response = await this.apiClient.addProductPicture(
              url?.result,
              image[i]?.name,
              Itemcode,
              this.username,
            );
            if (!response.hasError) {
              const blobClient = new BlockBlobClient(url.result || '');
              await blobClient.uploadData(image[i], {
                onProgress: (observer: any) => {
                  var progress = Math.round((observer.loadedBytes / image[i]?.size) * 100);
                  this.setState({ ImagePercent: progress / 100 });
                },
                blobHTTPHeaders: {
                  blobContentType: image[i]?.type,
                },
              });
            }
          }
        }
        this.setState({ imageProgress: false });
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('IMAGES_UPLOADED'));
        this.refresh(null, null, null);
      }
    } catch (error: any) {
      this.setState({ imageProgress: false });
    }
  };
  CallRemoveOrUpload = () => {
    // this.imageRemove(undefined,"")
    this.Removeimages();
  };

  // image = (Imgfile: any) => {
  //   this.setState({ image: Imgfile });
  //   const newImages = Imgfile.map((file: any) => {
  //     const url = URL.createObjectURL(file);
  //     return { file, url };
  //   });
  //   const newImageURLs = newImages.map((img: any) => img.url);
  //   this.setState((prevState: any) => ({
  //     imageFileAndUrls: newImages,
  //     imageURL:
  //       prevState.imageURL.length !== 0
  //         ? [...(prevState.imageURL || []), ...newImageURLs]
  //         : newImageURLs,
  //   }));
  //   debugger;
  //   this.state.image.forEach((img:any) => {
  //     this.prevImage.push(img);
  //   })
  // };

  image = (Imgfile: any) => {
    this.setState({ image: Imgfile }, () => {
      this.state.image.forEach((img: any) => {
        this.prevImage.push(img);
      });
      const newImages = Imgfile.map((file: any) => {
        const url = URL.createObjectURL(file);
        return { file, url };
      });
      const newImageURLs = newImages.map((img: any) => img.url);
      this.setState((prevState: any) => ({
        imageFileAndUrls:
          prevState.imageFileAndUrls.length !== 0
            ? [...(prevState.imageFileAndUrls || []), ...newImages]
            : newImages,
        imageURL:
          prevState.imageURL.length !== 0
            ? [...(prevState.imageURL || []), ...newImageURLs]
            : newImageURLs,
      }));
    });
  };

  handleDrop = (event: any) => {
    event.preventDefault();
    event.stopPropagation();
    const files = Array.from(event.dataTransfer.files);
    this.setState({ droppedFiles: files });
  };

  handleDragOver = (event: any) => {
    event.preventDefault();
    event.stopPropagation();
  };

  CheckState = () => {
    if (this.state.selectedRowData != null) {
      const check = this.state.selectedRowData.some((e: any) => {
        return e.photoShootStatus == 2;
      });
      if (check) {
        this.setState({ Imsubmit: false });
      } else {
        this.setState({ Imsubmit: true });
      }
    }
  };
  CheckStockState = () => {
    if (this.state.selectedRowData.length != 0) {
      if (this.state.selectedRowData.length == 1) {
        const check = this.state.selectedRowData.some((e: any) => {
          return e.stockReturnToQC == '1';
        });
        const check1 = this.state.selectedRowData.some((e: any) => {
          return e.stockReturnToQC == null || e.stockReturnToQC == '0';
        });
        if (check) {
          this.setState({ Stucksubmit: false });
        } else {
          this.setState({ Stucksubmit: true });
        }
        if (check1) {
          this.setState({ Stucksubmit1: true });
        } else {
          this.setState({ Stucksubmit1: false });
        }
      } else {
        this.setState({ Stucksubmit1: false, Stucksubmit: false });
      }
      // this.notify.showSuccessNotify(this.props.t(''));
    }
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { Search } = this.state;
    var searchResult = Search.filter((element: any) => {
      return element?.itemCode.toString().includes(newValue.trim());
    });
    this.setState({
      PSTracking: searchResult,
      searchKey: newValue,
    });
  }
  InvertDate = async () => {
    try {
      this.setState({ Imagedate: false });
      if (this.state.itemcode != null && this.state.DatePicker != null) {
        const Itemcode = parseInt(this.state.itemcode, 10);
        const utcDate = new Date(
          this.state.DatePicker.getTime() - this.state.DatePicker.getTimezoneOffset() * 60000,
        );
        const inwardDate = utcDate.toISOString();
        await this.apiClient.updateInwardDate(Itemcode, inwardDate);
        this.refresh(null, null, null);
      }
    } catch (error: any) {
      console.error(error.message);
    }
  };

  SavePhotoshoot = async () => {
    this.setState({ saveData: false, submitstockmodel: false });
    let message = '';
    let Common = 'Please complete ';
    let catologShot = 'catologShot';
    let individualStyle = 'individualStyle';
    let separete = ', ';
    let link = ' and ';
    let rangeShot = 'rangeShot';
    let pictureid = 'pictureid';
    const threeFieldCombinations = [
      [individualStyle, catologShot, rangeShot],
      [individualStyle, rangeShot, pictureid],
      [individualStyle, catologShot, pictureid],
      [rangeShot, catologShot, pictureid],
    ];
    const threeFieldGRNCombinations = [
      [individualStyle, catologShot, rangeShot],
    ];
    const twoFieldGRNCombinations = [
      [individualStyle, rangeShot],
      [individualStyle, catologShot],
      [rangeShot, catologShot],
    ];
    const twoFieldCombinations = [
      [individualStyle, rangeShot],
      [individualStyle, catologShot],
      [individualStyle, pictureid],
      [rangeShot, catologShot],
      [rangeShot, pictureid],
      [catologShot, pictureid],
    ];
    for (const element of this.state.selectedRowData) {
      if (element.productreceived == null) {
        message = this.props.t('ITEMCODE_ACKNOWLEDGE');
      } else {
        if (
          (element?.catologShot == null || element?.catologShot == undefined) &&
          (element?.individualStyle == null || element?.individualStyle == undefined) &&
          (element?.rangeShot == null || element?.rangeShot == undefined) &&
          (element?.pictureid == null ||
            element?.pictureid == undefined ||
            element?.pictureid == '')
        ) {
          if (element.imagetype == 'GRN')
          {
            message = this.props.t('GRN_ERROR_MESSAGE');
          }
          else{
            message = this.props.t('PHOTOSHOOT_SUBMIT_ERROR');
          }
        } else {
          let missingFields: any[] = [];
          if(element.imagetype == 'GRN')
          {
            for (const combination of threeFieldGRNCombinations) {
              if (
                combination.every(
                  (field) =>
                    element[field] == null || element[field] == undefined || element[field] === '',
                )
              ) {
                missingFields = missingFields.concat(combination);
                break;
              }
            }
          }
          else
          {
            for (const combination of threeFieldCombinations) {
              if (
                combination.every(
                  (field) =>
                    element[field] == null || element[field] == undefined || element[field] === '',
                )
              ) {
                missingFields = missingFields.concat(combination);
                break;
              }
            }
          }
          if (missingFields.length === 0) {
            if(element.imagetype == 'GRN')
            {
              for (const combination of twoFieldGRNCombinations) {
                if (
                  combination.every(
                    (field) =>
                      element[field] === null ||
                      element[field] === undefined ||
                      element[field] === '',
                  )
                ) {
                  missingFields = missingFields.concat(combination);
                  break;
                }
              }
            }
            else
            {
              for (const combination of twoFieldCombinations) {
                if (
                  combination.every(
                    (field) =>
                      element[field] === null ||
                      element[field] === undefined ||
                      element[field] === '',
                  )
                ) {
                  missingFields = missingFields.concat(combination);
                  break;
                }
              }
            }
          }
          if(element.imagetype == 'GRN')
          {
            if (missingFields.length === 0) {
              for (const field of [individualStyle, rangeShot, catologShot]) {
                if (
                  (element[field] === null ||
                    element[field] === undefined ||
                    element[field] === '') &&
                  !missingFields.includes(field)
                ) {
                  missingFields.push(field);
                  break;
                }
              }
            } 
          }
          else{
            if (missingFields.length === 0) {
              for (const field of [individualStyle, rangeShot, catologShot, pictureid]) {
                if (
                  (element[field] === null ||
                    element[field] === undefined ||
                    element[field] === '') &&
                  !missingFields.includes(field)
                ) {
                  missingFields.push(field);
                  break;
                }
              }
            }
          }

          if (missingFields.length > 1) {
            message =
              Common + missingFields.slice(0, -1).join(separete) + link + missingFields.slice(-1);
              if(element.imagetype !== 'GRN')
              {
                if (message.includes('pictureid')) {
                  message = message.replace('pictureid', 'upload image.');
                }
              }
            if (message.includes('individualStyle')) {
              message = message.replace(
                'individualStyle',
                this.props.t('COL_ACTION_INDIVIDUAL_SHOT'),
              );
            }
            if (message.includes('catologShot')) {
              message = message.replace('catologShot', this.props.t('COL_CATALOG_SHOT'));
            }
            if (message.includes('rangeShot')) {
              message = message.replace('rangeShot', this.props.t('COL_RANGE_SHOT'));
            }
            break;
          } else if (missingFields.length === 1) {
            const missingField = missingFields[0];
            switch (missingField) {
              case individualStyle:
                message = this.props.t('PHOTOSHOOT_SUBMIT_INDIVIDUALSTYLE');
                break;
              case rangeShot:
                message = this.props.t('PHOTOSHOOT_SUBMIT_RANGESHOT');
                break;
              case catologShot:
                message = this.props.t('PHOTOSHOOT_SUBMIT_CATOLOGSHOT');
                break;
              case pictureid:
                if (element.imagetype !== 'GRN') {
                  message = this.props.t('PHOTOSHOOT_SUBMIT_PICTURE');
                }
                break;
              default:
                break;
            }
          }
        }
      }
    }

    if (message.trim() !== '') {
      this.notify.showErrorNotify(message.trim());
      this.refresh(null, null, null);
      return;
    }

    if (this.state.submit) {
      for (const element of this.state.selectedRowData) {
        await this.apiClient.updateProductImage(
          element.itemCode,
          undefined,
          4,
          undefined,
          undefined,
          this.username,
        );
      }
      this.notify.showSuccessNotify(this.props.t('SUBMITTED_SUCCESSFULLY'));
      this.refresh(null, null, null);
    } else if (this.state.submitstock) {
      if (this.state.selectedRowData.length != 0) {
        this.setState(
          (prevState: any) => {
            if (prevState.rangeShotopt != null) {
              const updatedData = prevState.selectedRowData.map((item: any) => {
                return { ...item, comment: prevState.otherSplitComment1 };
              });
              return { selectedRowData: updatedData };
            }
            return null;
          },
          () => {},
        );
        this.state.selectedRowData.forEach(async (e: any) => {
          await this.apiClient.updateProductImage(
            e.itemCode,
            undefined,
            5,
            this.state.otherSplitComment1,
            undefined,
            this.username,
          );
        });
        this.notify.showSuccessNotify(this.props.t('SUBMITTED_SUCCESSFULLY'));
        this.refresh(null, null, null);
      } else {
        await this.apiClient.updateProductImage(
          this.state.itemcode,
          undefined,
          5,
          this.state.otherSplitComment1,
          undefined,
          this.username,
        );
        this.refresh(null, null, null);
      }
    }
  };
  SavePhotoshootRIC = async (removeData: any) => {
    this.setState({ approveVisible1: false, approveVisible2: false });
    if (this.state.itemcode != null) {
      if (this.state.catolog == false && this.state.individual == true) {
        await this.apiClient.updateProductImage(
          this.state.itemcode,
          undefined,
          3,
          undefined,
          removeData,
          this.username,
        );
        this.refresh(null, null, null);
      } else if (this.state.catolog == true && this.state.individual == false) {
        await this.apiClient.updateProductImage(
          this.state.itemcode,
          undefined,
          2,
          undefined,
          removeData,
          this.username,
        );
        this.refresh(null, null, null);
      } else if (this.state.rangeShotopt != null) {
        await this.apiClient.updateProductImage(
          this.state.itemcode,
          this.state.rangeShotopt,
          1,
          undefined,
          undefined,
          this.username,
        );
        this.refresh(null, null, null);
      }
    }
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };

  closingFullImageModal = () => {
    this.setState({ fullImageVisible: false, fullImageSrc: '' });
  };

  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  normalizeValue(value: any) {
    if (value === null || value === undefined) {
      return '';
    }
    if (typeof value === 'string') {
      return value.trim();
    }
    return value;
  }
  setVisible(data: any) {
    this.setState({ ImageFolder: data }); //, imgVisible: false });
  }
  imageRemove = (index: any, imageUrl: string) => {
    if (
      this.state.AllImageUrl &&
      this.state.AllImageUrl.length !== 0 &&
      this.state.AllUrl &&
      this.state.AllUrl.length !== 0
    ) {
      this.setState(
        (prevState: any) => {
          const remainingImages = prevState.AllUrl.filter((image: any, i: number) => i !== index);
          const remainingViewImages = prevState.imageURL.filter(
            (image: any, i: number) => i !== index,
          );
          const splitImgUrl = prevState.AllImageUrl.filter((url: string) => {
            const findurl = url.split('?')[0];
            return findurl === imageUrl;
          });
          const findIds = prevState.pictureData
            .filter((e: any) => splitImgUrl.includes(e.imageUrl))
            .map((e: any) => e.id);
          const images = this.state.imageFileAndUrls;
          const matchingImages = images.filter((img: any) =>
            remainingViewImages.some((url: string) => img.url.includes(url)),
          );

          const newImgs = matchingImages.map((img: any) => img.file);
          return {
            AllUrl: remainingImages,
            imageURL: remainingViewImages,
            RemovedimageId: [...prevState.RemovedimageId, ...findIds],
            image: newImgs,
          };
        },
        () => {
          this.prevImage = this.state.image;
        },
      );
    } else if (this.state.imageURL.length !== 0) {
      this.setState(
        (prevState: any) => {
          const remainingViewImages = prevState.imageURL.filter(
            (image: any, i: number) => i !== index,
          );
          const images = this.state.imageFileAndUrls;
          const matchingImages = images.filter((img: any) =>
            remainingViewImages.some((url: string) => img.url.includes(url)),
          );
          const newImgs = matchingImages.map((img: any) => img.file);
          return {
            imageURL: remainingViewImages,
            image: newImgs,
          };
        },
        () => {
          this.prevImage = this.state.image;
        },
      );
    }
  };
  Removeimages = async () => {
    try {
      this.setState({ imgVisible: false });
      if (this.state.RemovedimageId.length != 0) {
        const removedImageIds = this.state.RemovedimageId;
        const ImagesIds = removedImageIds.reverse().join(',');
        const Itemcode = parseInt(this.state.itemcode, 10);
        await this.apiClient.removePicture(Itemcode, ImagesIds, this.username).then(() => {
          this.notify.showSuccessNotify1(this.props.t('IMAGE_REMOVED'));
        });
      }
      var image = this.prevImage;
      if (image.length != 0) {
        await this.UploadPicture();
      }
      this.refresh(null, null, null);
    } catch (error: any) {
      this.notify.showErrorNotify(error.message);
    }
  };
  Callme = () => {
    this.setState({
      approveVisible1: false,
      selectedRowData: [],
      noSelection: !this.state.noSelection,
    });
    this.setState(
      (prevState: any) => {
        const statusKey = parseInt(prevState.itemcode, 10);
        let updatedData = prevState.PSTracking.map((item: any) => {
          if (item.itemCode === statusKey) {
            if (prevState.individualStyle && prevState.individual) {
              return { ...item, individualStyle: 'yes' };
            } else if (prevState.catologShot && prevState.catolog) {
              return { ...item, catologShot: 'yes' };
            }
            if (prevState.rangeShotopt != null) {
              return { ...item, rangeShot: prevState.rangeShotopt };
            }
          }
          return item;
        });
        return { PSTracking: updatedData, validatedData: updatedData };
      },
      () => {},
    );
  };

  StackreturnUpdate = () => {
    if (this.state.selectedRowData.length != 0) {
      this.setState({ submitstockmodel: true });
      // const stock = this.state.selectedRowData.filter((e: any) => {
      //   return e.itemCode == Itemcode;
      // });
      // if (stock) {

      // }
    } else {
      this.notify.showErrorNotify(this.props.t('PHOTOSHOOT_STOCKSUBMIT_ERROR'));
      this.setState({ selectedRowData: [], submitstockmodel: false, saveData: false });
      this.refresh(null, null, null);
      return;
    }
  };

  Action = async () => {
    let response: any;
    let rejectComment = this.state.otherSplitComment;
    if (this.state.approveVisible) {
      this.setState({ approveVisible: false });
      const Approve = 'Received';
      const Itemcode = parseInt(this.state.itemcode, 10);
      response = await this.apiClient.updatePhotoshotstate(
        Itemcode,
        Approve,
        undefined,
        this.username,
      );
    } else if (this.state.recjectVisible) {
      this.setState({ recjectVisible: false });
      const reject = 'Photoshoot';
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
    this.refresh(null, null, null);
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
    const { t } = this.props;
    let { returnopt1 } = this.state;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        minWidth: '70px',
      },
      {
        key: 'itemcode',
        name: <div className={columnHeader}>{`${this.props.t('ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        isVisible: true,
        minWidth: '140px',
      },
      {
        key: 'eancode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eancode}>{row.eancode}</span>,
        isVisible: true,
        minWidth: '140px',
      },
      {
        key: 'action',
        name: <div className={columnHeader}>{`${this.props.t('COL_ACTION')}`}</div>,
        selector: (row: any) => {
          return (
            <>
              {this.state.PSTracking.length !== 0 && (
                <Grid container direction={'row'} spacing={2}>
                  {(row.productreceived == 'Received' || row.productreceived == null) && (
                    <Grid item>
                      <IconButton
                        styles={
                          row.productreceived !== null
                            ? row.productreceived == 'Received'
                              ? iconButtonStylesssClicked
                              : iconButtonStyless
                            : iconButtonStylesssClicked1
                        }
                        iconProps={approveIcon}
                        //disabled={row.productreceived == 'Received'}
                        onClick={() => {
                          if (row.productreceived == null) {
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
                  {(row.productreceived == 'no' || row.productreceived == null) && (
                    <Grid item>
                      <IconButton
                        styles={
                          row.productreceived !== null
                            ? row.productreceived == 'no'
                              ? iconButtonStyless1
                              : iconButtonStylesssClicked
                            : iconButtonStyless
                        }
                        iconProps={rejectIcon}
                        disabled={row.imagetype == 'GRN'}
                        onClick={() => {
                          if (row.productreceived == null) {
                            this.setState({
                              itemcode: row.itemCode,
                              recjectVisible: true,
                              approveVisible: false,
                            });
                          }
                        }}
                      />
                    </Grid>
                  )}
                </Grid>
              )}
            </>
          );
        },
        isVisible: true,
        minWidth: '150px',
      },
      {
        key: 'action_individual_style',
        name: <div className={columnHeader}>{`${this.props.t('COL_ACTION_INDIVIDUAL_SHOT')}`}</div>,
        selector: (row: any) => {
          if (row.individualStyle == null || row.individualStyle != null) {
            if (this.state.PSTracking.length != 0) {
              return (
                <>
                  {this.state.PSTracking.length != 0 && (
                    <Grid container direction={'row'} spacing={2}>
                      <Grid item>
                        <IconButton
                          styles={
                            row.individualStyle === 'yes'
                              ? iconButtonStylesssClicked
                              : iconButtonStylesss
                          }
                          disabled={row.productreceived == null}
                          iconProps={approveIcon}
                          onClick={() => {
                            row.photoShootStatus == '1'
                              ? this.setState({
                                  approveVisible1: true,
                                  alreadyselected: row.individualStyle === 'yes' ? true : false,
                                  isindividualStyle: true,
                                  iscatolog: false,
                                  individualStyle: true,
                                  individual: true,
                                  catolog: false,
                                  itemcode: row.itemCode,
                                })
                              : null;
                          }}
                        />
                      </Grid>
                    </Grid>
                  )}
                </>
              );
            }
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '120px',
      },
      {
        key: 'catalog_shot',
        name: <div className={columnHeader}>{`${this.props.t('COL_CATALOG_SHOT')}`}</div>,
        selector: (row: any) => {
          if (row.catologShot == null || row.catologShot != null) {
            if (this.state.PSTracking.length != 0) {
              return (
                <>
                  {this.state.PSTracking.length != 0 && (
                    <Grid container direction={'row'} spacing={2}>
                      <Grid item>
                        <IconButton
                          styles={
                            row.catologShot === 'yes'
                              ? iconButtonStylesssClicked
                              : iconButtonStylesss
                          }
                          iconProps={approveIcon}
                          disabled={row.productreceived == null}
                          onClick={() =>
                            row.photoShootStatus == '1'
                              ? this.setState({
                                  approveVisible2: true,
                                  catolog: true,
                                  alreadyselectedCatlog: row.catologShot == 'yes' ? true : false,
                                  individualStyle: false,
                                  iscatolog: true,
                                  individual: false,
                                  catologShot: true,
                                  itemcode: row.itemCode,
                                })
                              : null
                          }
                        />
                      </Grid>
                    </Grid>
                  )}
                </>
              );
            }
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'link',
        name: <div className={columnHeader}>{t('IMAGE')}</div>,
        selector: (row: any) => {
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
                  <Icon iconName="Link" title={t('Open Link')} className="linkIcon" />
                </a>
              </div>
            );
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'ps_image',
        name: <div className={columnHeader}>{`${this.props.t('COL_PS_IMAGE')}`}</div>,
        selector: (row: any) => {
          if (row.itemCode) {
            var matchingUrls: { map: (arg0: (url: string) => string) => string | null };
            var ImgUrl: string | null;
            if (row.pictureid !== null) {
              if (row.pictureid != '') {
                const pictureIdsArray = row.pictureid
                  .split(',')
                  .map((id: any) => parseInt(id.trim(), 10));
                const matchingObjects = this.state.pictureData.filter((e: any) =>
                  pictureIdsArray.includes(e.id),
                );
                matchingUrls = matchingObjects.map((obj: any) => obj.imageUrl);
                ImgUrl = matchingUrls.map((url: string) => url.split('?')[0]);
              } else {
                ImgUrl = '';
              }
            } else {
              ImgUrl = '';
            }
            return (
              <Icon
                iconName="PhotoCollection"
                title={t('COL_PS_IMAGE')}
                className="iconStyle"
                onClick={() => {
                  // row.photoShootStatus == 1 &&
                  this.setState({
                    imgVisible: true,
                    itemcode: row.itemCode,
                    imageURL: ImgUrl,
                    imageLoading: ImgUrl === '' ? true : false,
                    //row.stockReturnToQC !== null &&
                    uploadoption: row.photoShootStatus == 2 ? true : false,
                  });
                  row.productreceived == 'Received' &&
                    this.setState({
                      AllImageUrl: matchingUrls,
                      AllUrl: ImgUrl,
                      Browseenable:
                        row.productreceived != null && row.photoShootStatus != 2 ? true : false,
                      imgVisible: true,
                      itemcode: row.itemCode,
                      imageURL: ImgUrl,
                      imageLoading: ImgUrl === '' ? true : false,
                      // row.stockReturnToQC == 1 &&
                      uploadoption: row.photoShootStatus == 2 ? true : false,
                    });
                }}
              />
            );
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '80px',
      },
      // {
      //   key: 'division',
      //   name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
      //   selector: (row: any) => {
      //     if (row.divisionId) {
      //       var division1 = null;
      //       const DivKey = parseInt(row.divisionId, 10);
      //       const statusObject = this.state.deptOption
      //         ? this.state.deptOption.find((element: any) => element.key === DivKey)
      //         : null;
      //       if (statusObject) {
      //         division1 = statusObject.text;
      //       }
      //       return (
      //         <>
      //           <span>{division1}</span>
      //         </>
      //       );
      //     }
      //   },
      //   isVisible: true,
      //   minWidth: '130px',
      // },
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
                options={this.state.deptOption}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    searchKey: '',
                  });
                  this.DivisitonValue = selectedOption.key;
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
              {this.state.divisionSelected && (
                <IconButton
                  style={{ marginLeft: 10, marginTop: '3%' }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.DivisitonValue = [];
                    this.setState({ isDivision: false, divisionSelected: '' });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => <span title={row.division}>{row.division}</span>,
        position: 'fixed',
        isVisible: true,
        width: '140px',
      },
      {
        key: 'type',
        name: <div className={columnHeader}>{`${this.props.t('TYPE')}`}</div>,
        selector: (row: any) => <span title={row.imagetype}>{row.imagetype}</span>,
        width: '140px',
        isVisible: true,
      },
      {
        key: 'date_received',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => <span title={row.receivedDate}>{row.receivedDate}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'priority',
        name: <div className={columnHeader}>{`${this.props.t('COL_PRIORITY')}`}</div>,
        selector: (row: any) => <span title={row.priority}>{row.priority}</span>,
        isVisible: true,
        minWidth: '90px',
      },
      {
        key: 'studio_name',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_NAME')}`}</div>,
        selector: (row: any) => <span title={row.studioName}>{row.studioName}</span>,
        isVisible: true,
        minWidth: '160px',
      },
      {
        key: 'studio_location',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_LOCATION')}`}</div>,
        selector: (row: any) => <span title={row.studioLocation}>{row.studioLocation}</span>,
        isVisible: true,
        minWidth: '150px',
      },
      {
        key: 'inward_date',
        name: <div className={columnHeader}>{`${this.props.t('COL_INWARD_DATE')}`}</div>,
        selector: (row: any) => {
          if (this.state.PSTracking.length !== 0) {
            if (row.photoShootStatus == 2) {
              return (
                <div title={`${row.inwardDate}`} style={{ textAlign: 'center' }}>
                  {row.inwardDate}
                </div>
              );
            }
            if (row.productreceived != null) {
              return (
                <>
                  <div style={{ display: 'flex', justifyContent: 'center' }}>
                    <DatePicker
                      className="datePicker"
                      showMonthPickerAsOverlay={true}
                      formatDate={(e: any) => e.toLocaleDateString('en-GB')}
                      onSelectDate={(date: Date | null | undefined) => {
                        if (date !== undefined && date !== null) {
                          const selectedDate = new Date(
                            date.getFullYear(),
                            date.getMonth(),
                            date.getDate(),
                          );
                          this.setState(
                            {
                              DatePicker: selectedDate,
                              itemcode: row.itemCode,
                              Imagedate: true,
                            },
                            this.InvertDate,
                          );
                        }
                      }}
                      showGoToToday={true}
                    />
                    <div
                      style={{ marginRight: '10px', marginTop: '10px' }}
                      title={`Inward Date: ${row.inwardDate}`}
                    >
                      {row.inwardDate}
                    </div>
                  </div>
                </>
              );
            }
          }
          return null;
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'photoshoot_status',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('COL_PHOTOSHOOT_STATUS')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={[
                  { key: '1', text: 'Pending' },
                  { key: '2', text: 'Completed' },
                  { key: '3', text: 'Closed' },
                ]}
                // options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.PTstatus = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    PSstatusopt: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
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
              {this.state.PSstatusopt && (
                <IconButton
                  style={{ marginLeft: -3, marginTop: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.PTstatus = [];
                    this.setState({ isDivision: false, PSstatusopt: '' });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          switch (row.photoShootStatus) {
            case '1':
              return (
                <span className={`customer-badge status-Pending`}>
                  <span title={`${'Pending'}`}>{'Pending'}</span>
                </span>
              );
            case '2':
              return (
                <span className={`customer-badge status-Approved`}>
                  <span title={`${'Completed'}`}>{'Completed'}</span>
                </span>
              );
            case '3':
              return (
                <span className={`customer-badge status-Closed`}>
                  <span title={`${'Closed'}`}>{'Closed'}</span>
                </span>
              );
            default:
              return <span></span>;
          }
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'stock_return_to_qc',
        // name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={[
                  { key: '1', text: 'Yet To Return' },
                  { key: '2', text: 'Returned' },
                ]}
                // options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.SRstatus = selectedOption.key;
                  this.setState({
                    isDivision: true,
                    stockqc: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                selectedKey={this.state.stockqc}
                styles={columnFilter}
                style={{ marginLeft: -48, marginTop: 18, }}
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
              {this.state.stockqc && (
                <IconButton
                  style={{ marginLeft: -1, marginTop: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.SRstatus = [];
                    this.setState({ isDivision: false, stockqc: '' });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          if (this.state.PSTracking.length !== 0) {
            let stockval;
            const statusKey = parseInt(row.stockReturnToQC, 10);
            if (row.stockReturnToQC != null && statusKey != 0) {
              const statusObject = this.state.StockReturnToqc.find(
                (element: any) => element.key === statusKey,
              );
              if (statusObject) {
                stockval = statusObject.text;
              }
              return (
                <>
                  <span title={stockval}>{stockval}</span>
                  <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
                    {row.comments != null ? (
                      <FaRegClone
                        onClick={() => {
                          this.setState({
                            recjected: true,
                            descriptionValue1: row.comments,
                          });
                        }}
                      />
                    ) : (
                      ''
                    )}
                  </span>
                </>
              );
            } else {
              if (row.stockReturnToQC == null || statusKey == 0) {
                return (
                  <>
                    <Dropdown
                      styles={PIMDropdownStyles3}
                      options={this.state.StockReturnToqc}
                      placeholder={`${this.props.t('YET TO RETURN')}`}
                      selectedKey={returnopt1[`returnopt${row?.itemCode}`]}
                      disabled={row.productreceived === 'no' || row.productreceived === null}
                      onChange={(event, option) => {
                        if (option) {
                          if (option.key === 1) {
                            if (row.photoShootStatus === '2') {
                              this.setState({
                                returnopt1: {
                                  ...returnopt1,
                                  ...{ [`returnopt${row?.itemCode}`]: option.key },
                                },
                              });
                              this.setState({
                                itemcode: row.itemCode,
                                returnopt: option.key,
                                submit: false,
                                submitstockmodel: true,
                                submitstock: true,
                                selectedRowData: [],
                              });
                            } else {
                              this.setState({
                                returnopt1: {
                                  ...returnopt1,
                                  ...{ [`returnopt${row?.itemCode}`]: 0 },
                                },
                              });

                              this.notify.showErrorNotify(
                                this.props.t('STOCK_RETURN_BEFORE_PS_ERROR'),
                              );
                            }
                          } else {
                            this.setState({
                              returnopt1: {
                                ...returnopt1,
                                ...{ [`returnopt${row?.itemCode}`]: option.key },
                              },
                            });
                            this.setState({
                              itemcode: row.itemCode,
                              returnopt: option.key,
                              submit: false,
                              submitstockmodel: false,
                              submitstock: false,
                              selectedRowData: [],
                            });
                          }
                        }
                      }}
                    />
                  </>
                );
              }
            }
          }
          return null;
        },
        isVisible: true,
        minWidth: '180px',
      },
      {
        key: 'stock_return_date',
        name: <div className={columnHeader}>{`${this.props.t('COL_STOCKRETURN_DATE')}`}</div>,
        selector: (row: any) => {
          if (row.stockReturnDate) {
            return (
              <>
                <div title={`${row.stockReturnDate}`}>
                  <Grid lg={12} item container direction={'row'}>
                    {row.stockReturnDate}
                  </Grid>
                </div>
              </>
            );
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '150px',
      },
      {
        key: 'range_shot',
        name: <div className={columnHeader}>{`${this.props.t('COL_RANGE_SHOT')}`}</div>,
        selector: (row: any) => {
          let keyOption = row.rangeShot == null ? null : row.rangeShot === 'Not Applicable' ? 0 : 1;
          if (this.state.PSTracking.length !== 0) {
            if (row.photoShootStatus == 2) {
              return (
                <div title={row.rangeShot} style={{ textAlign: 'center' }}>
                  {row.rangeShot}
                </div>
              );
            }
            if (row.productreceived != null) {
              return (
                <>
                  {/* <ComboBox
                    placeholder={t('DROPDOWN_PLACEHOLDER')}
                    styles={PIMDropdownStyles4}
                    options={this.state.RangeShotDrop}
                    disabled={row.productreceived == 'no' || row.productreceived == null}
                    onChange={(e: any, option: any) => {
                      this.setState(
                        {
                          rangeShotopt: option.key,
                          itemcode: row.itemCode,
                          rangeenable: true,
                          individual: false,
                          catolog: false,
                        },
                        () => this.SavePhotoshootRIC(),
                      );
                    }}
                  /> */}
                  <div style={{ display: 'flex', justifyContent: 'center' }}>
                    <Grid container spacing={1} alignItems="center">
                      <Grid item>
                        <ComboBox
                          placeholder={t('DROPDOWN_PLACEHOLDER')}
                          styles={PIMDropdownStyles}
                          options={this.state.RangeShotDrop}
                          defaultSelectedKey={keyOption}
                          disabled={row.productreceived == 'no' || row.productreceived == null}
                          onChange={(e: any, option: any) => {
                            this.setState(
                              {
                                rangeShotopt: option.key,
                                itemcode: row.itemCode,
                                rangeenable: true,
                                individual: false,
                                catolog: false,
                              },
                              () => this.SavePhotoshootRIC(undefined),
                            );
                          }}
                        />
                      </Grid>
                    </Grid>
                  </div>
                </>
              );
            } else {
              return null;
            }
          }
        },
        isVisible: true,
        minWidth: '167px',
      },
    ];
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    // if (
    //   this.state.selectedOptionIm == 1 ||
    //   this.state.selectedOptionIm == 2 ||
    //   this.state.selectedOptionIm == 3 ||
    //   this.state.selectedOptionIm == 4
    // ) {
    //   const index = columns.findIndex((column: any) => column.key === 'catalog_shot');
    //   columns.splice(index + 1, 0, {
    //     key: 'link',
    //     name: <div className={columnHeader}>{t('PLANOGRAM_TABLE_LINK')}</div>,
    //     selector: (row: any) => {
    //       if (row.imageUrl) {
    //         if (
    //           this.state.selectedOptionIm == 1 ||
    //           this.state.selectedOptionIm == 2 ||
    //           this.state.selectedOptionIm == 3 ||
    //           this.state.selectedOptionIm == 4
    //         ) {
    //           return (
    //             <a href={row.imageUrl} target="_blank" rel="noopener noreferrer">
    //               <Icon iconName="Link" title={t('Open Link')} className="linkIcon" />
    //             </a>
    //           );
    //         }
    //       } else {
    //         return null;
    //       }
    //     },
    //     isVisible: true,
    //     minWidth: '80px',
    //   });
    // }

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
                >{`${this.props.t('COL_PHOTOSHOOT_TRACKING')}`}</Text> */}
            </Grid>
            <Grid item lg={1} xs={2} />
            <Grid item lg={0.4} xs={1.8}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.PSTracking.length !== 0) {
                    this.downloadFile(this.state.fromDate, this.state.toDate, this.state.imageType);
                  }
                }}
              />
            </Grid>
          </Grid>
        </Stack>
        <Grid lg={12} item container spacing={0.8} direction={'row'}>
          <Grid item lg={0.1} xs={6} />
          <Grid item lg={2} xs={12}>
            <SearchBox
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ITEMCODE')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={1.96} xs={6}>
            <Dropdown
              options={this.state.IMdropdownOptions}
              styles={PIMDropdownStyles}
              placeholder={`${t('DROPDOWN_IMAGE_TYPE')}`}
              key={this.state.refreshKey}
              selectedKey={this.state.optionSelected}
              // defaultSelectedKey={4}
              // selectedKey={this.state.selectedOptionIm}
              onChanged={(option) => {
                this.setState({ selectedOptionIm: option.key, optionSelected: option.key });
                this.Imageoption = option.text;
                // this.columnFilter();
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
          </Grid>
          <Grid item lg={0.5} xs={6}>
            {/* <Icon
              iconName="WaitlistConfirm"
              title={this.props.t('ADD_BTN_SUBMIT')}
              className="iconStyle"
              onMouseOver={() => this.setState({ savingValue: true })}
              onMouseOut={() => this.setState({ savingValue: false })}
              onClick={() => {
                this.CheckState();
                this.CheckStockState();
                this.setState({ saveData: true });
              }}
            /> */}
            <span title={this.props.t('ADD_BTN_SUBMIT')}>
              <DocumentCheckmarkFilled
                className="submitIconStyle"
                onClick={() => {
                  if (this.state.selectedRowData.length === 0) {
                    return;
                  }

                  this.CheckState();
                  this.CheckStockState();
                  this.setState({ saveData: true });
                }}
              />
            </span>
          </Grid>
          <Grid item lg={4.04} xs={7.8}></Grid>
          <Grid item lg={0.5} xs={1.8}>
            <Icon
              iconName="Search"
              title={t('BTN_SEARCH')}
              className="iconStyle"
              onClick={() => {
                if (this.state.fromDate || this.state.toDate) {
                  this.refresh(this.state.fromDate, this.state.toDate, this.state.optionSelected);
                } else {
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
                  this.state.searchKey !== '' ||
                  this.state.optionSelected.length != 0
                ) {
                  this.setState({
                    fromDate: null,
                    toDate: null,
                    optionSelected: [],
                    searchKey: '',
                  });
                  this.refresh(null, null, null);
                  this.ClearDates();
                }
                this.Imageoption = 'All';
                // else if (this.state.searchKey !== ''){
                //   this.setState({ fromDate: null, toDate: null , searchKey: '' });
                //   this.refresh(null, null);
                //   this.ClearDates();
                // }
              }}
            />
          </Grid>
          <Grid item lg={0.1} xs={1.4}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={async () => {
                this.setState({ fromDate: null, toDate: null, optionSelected: [] });
                this.ClearDates();
                this.refresh(null, null, null);
                this.Imageoption = 'All';
              }}
            />
          </Grid>
        </Grid>
        {this.state.selectedRowData.length > 0 && (
          <Modal
            isOpen={this.state.saveData}
            containerClassName={PIMcontentStyles.SaveContainer}
            onDismiss={() =>
              this.setState({
                saveData: false,
              })
            }
          >
            <div className={PIMcontentStyles.header2}>
              <IconButton
                styles={iconButtonStyles}
                iconProps={{ iconName: 'Cancel' }}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    saveData: false,
                  });
                }}
              />{' '}
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={0.6} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('Submit')}`}
                    disabled={
                      !(
                        this.state.selectedRowData.length != 0 &&
                        this.state.Submit1 == true &&
                        this.state.Imsubmit == true
                      )
                    }
                    onClick={() => {
                      this.setState({ submit: true, submitstock: false }, () => {
                        this.SavePhotoshoot();
                      });
                    }}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={5.59} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('SUBMIT_WITH_STOCK')}`}
                    disabled={
                      !(
                        (this.state.selectedRowData.length != 1 &&
                          this.state.Stucksubmit == true) ||
                        this.state.Stucksubmit1 == true
                      )
                    }
                    onClick={() => {
                      this.setState(
                        { submitstock: true, submitstockmodel: true, submit: false },
                        () => {
                          this.StackreturnUpdate();
                        },
                      );
                    }}
                  />
                </Grid>
                <Grid item lg={3.8} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonCancel}
                    text={`${this.props.t('BTN_CANCEL')}`}
                    onClick={() => this.setState({ saveData: false })}
                  />
                </Grid>
              </Grid>
            </div>
          </Modal>
        )}
        <Modal
          isOpen={this.state.submitstockmodel}
          containerClassName={PIMcontentStyles.container5}
          isBlocking={false}
          //  dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              submitstockmodel: false,
              otherSplitComment1: '',

              returnopt1: { ...returnopt1, ...{ [`returnopt${this.state?.itemcode}`]: 0 } },
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            {/* <Text variant="xLarge" className="apptext3">{`${this.props.t(
                'RETURN_COMMENT',
              )}`}</Text> */}
          </div>
          <div>
            <IconButton
              styles={iconButtonStyles}
              iconProps={{ iconName: 'Cancel' }}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({
                  submitstockmodel: false,
                  otherSplitComment1: '',

                  returnopt1: { ...returnopt1, ...{ [`returnopt${this.state?.itemcode}`]: 0 } },
                });
              }}
            />
          </div>
          <div className={PIMcontentStyles.body}>
            <form noValidate autoComplete="off">
              <TextField 
               style={{overflow:'hidden'}}
                name="otherComment"
                className="textField stocktxt"
                // required
                label={this.props.t('RETURN_COMMENT')}
                onChange={(event: any) =>
                  this.setState({
                    otherSplitComment1: event.target.value.trim().replace(/\s{2,}/g, ' '),
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
                  // disabled={this.state.otherSplitComment1.trim() == ''}
                  onClick={() => {
                    this.setState({ submitstock: true, submit: false });
                    this.SavePhotoshoot();
                    this.setState({ otherSplitComment1: '' });
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
                    this.setState({
                      submitstockmodel: false,
                      otherSplitComment1: '',
                      returnopt: 0,
                      returnopt1: { ...returnopt1, ...{ [`returnopt${this.state?.itemcode}`]: 0 } },
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.approveVisible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          // dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              approveVisible: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
          <IconButton
            styles={iconButtonStyles}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody5}>
            <Text variant="xLarge" className="apptext2 dlttxt">{`${this.props.t(
              'PHOTOSHOOT_TRACKING_MODEL_APPROVE',
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
                    this.Action();
                    this.setState({ Browseenable: true });
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
                    this.setState({ approveVisible: false });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        {/* <Modal
            isOpen={this.state.imgVisible}
            containerClassName={PIMcontentStyles.imageContainer}
            onDismiss={() =>
              this.setState({
                imgVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <span className="apptext3">{t('IMAGE')}</span> */}
        {/* <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => this.setState({ imgVisible: false })}
              />
               <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyle1"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={() => {
                    // this.downloadImage(this.state.imageDownload);
                  }}
                />
              </Grid> */}
        {/* </div>
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
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={this.state.uploadoption != true ? 1.4 : 4} xs={12}></Grid>
              {this.state.uploadoption !== true && (
                <Grid item lg={3} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('UPLOAD_IMAGE')}`}
                    onClick={() => {
                      this.setState({ ImageFolder: true });
                    }}
                  />
                </Grid>
              )}
              {this.state.uploadoption !== true && (
                <Grid item lg={3} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('BTN_CONFIRM')}`}
                    onClick={() => {
                      this.setState({ imgVisible: false });
                    }}
                  />
                </Grid>
              )}
              <Grid item lg={3} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ imgVisible: false });
                  }}
                />
              </Grid>
            </Grid>
          </Modal> */}
        <Modal
          isOpen={this.state.approveVisible1}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          // dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              approveVisible1: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
          <IconButton
            styles={UploadStyles}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible1: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody8}>
            <Text variant="xLarge" className="popupHeader">
              {this.state.alreadyselected
                ? this.props.t('INDIVIDUAL_STYLE_REMOVE')
                : this.props.t('INDIVIDUAL_STYLE_COMPLETED')}
            </Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />

              <Grid item lg={4} xs={12}>
                {this.state.alreadyselected == true ? (
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('Remove')}`}
                    onClick={() => {
                      this.SavePhotoshootRIC('no');
                    }}
                  />
                ) : (
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('BTN_CONFIRM')}`}
                    onClick={() => {
                      this.SavePhotoshootRIC('yes');
                    }}
                  />
                )}
              </Grid>

              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({
                      approveVisible1: false,
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.approveVisible2}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          // dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              approveVisible2: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
          <IconButton
            styles={UploadStyles}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible2: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody8}>
            <Text variant="xLarge" className="apptext2 dlttxt popupHeader">
              {this.state.alreadyselectedCatlog
                ? this.props.t('CATALOG_SHOT_REMOVE')
                : this.props.t('CATALOG_SHOT_COMPLETED')}
            </Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />

              <Grid item lg={4} xs={12}>
                {this.state.alreadyselectedCatlog == true ? (
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('Remove')}`}
                    onClick={() => {
                      this.SavePhotoshootRIC('no');
                    }}
                  />
                ) : (
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${this.props.t('BTN_CONFIRM')}`}
                    onClick={() => {
                      this.SavePhotoshootRIC('yes');
                    }}
                  />
                )}
              </Grid>

              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({
                      approveVisible2: false,
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.recjectVisible}
          containerClassName={PIMcontentStyles.container5}
          isBlocking={false}
          // dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              recjectVisible: false,
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ recjectVisible: false, row: [] })}
          />
          <div className={PIMcontentStyles.header}>
            <Text variant="xLarge" className="apptext3 rejpop">{`${this.props.t(
              'PLANOGRAM_TABLE_MODEL_REJECT',
            )}`}</Text>
          </div>
          <div className={PIMcontentStyles.body}>
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
                    this.setState({ Browseenable: true });
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
                      recjectVisible: false,
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.ImageFolder}
          containerClassName={PIMcontentStyles.ExcelContainer}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              ImageFolder: false,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <div className="apptext3">
              <h2>{t('UPLOAD_PICTURE')}</h2>
            </div>
            <IconButton
              styles={UploadimageStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ ImageFolder: false });
              }}
            />
          </div>
          <div className={PIMcontentStyles.body}>
            <PsUploadImageFolder
              onUploadImage={(Imgfile: any) => this.image(Imgfile)}
              ClosePopup={() => this.setVisible(false)}
            />
          </div>
        </Modal>
        {/* <Modal
            isOpen={this.state.stockerror}
            containerClassName={PIMcontentStyles.ExcelContainerPS}
            isBlocking={false}
            // dragOptions={this.dragOptions}
            onDismiss={() =>
              this.setState({
                stockerror: false,
              })
            }
          >
            <div className="PSsubmit">{t('Before selecting stock return Please do submit')}</div>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={10} xs={12} />
              <Grid item lg={10} xs={12} />
              <Grid item lg={3.6} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ stockerror: false });
                  }}
                />
              </Grid>
            </Grid>
          </Modal> */}
        <Modal
          isOpen={this.state.Imagedate}
          containerClassName={PIMcontentStyles.confirmContainer}
          isBlocking={false}
          //dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              Imagedate: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmbody}>
            <Text variant="xLarge" className="Pstext">{`${this.props.t(
              'INWARD_DATE_CONFIRMATION',
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
                  onClick={() => this.InvertDate()}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ Imagedate: false });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.recjected}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() => this.setState({ recjected: false })}
          isBlocking={false}
        >
          <div style={{ position: 'absolute', top: 0, right: 0, color: 'black' }}>
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ recjected: false });
              }}
            />
          </div>
          <br></br>
          <div className="statusComments">
            <form noValidate autoComplete="off">
              <TextField
                styles={rejectComment}
                className="textField stocktxt"
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
                this.prevImage = [];
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
                      {this.state.uploadoption !== true && (
                        <IconButton
                          styles={iconButtonStylesImg}
                          iconProps={cancelIcon}
                          ariaLabel="Close image"
                          onClick={() => {
                            this.imageRemove(index, imageUrl);
                          }}
                          style={{ position: 'absolute', top: '0', right: '14px' }}
                        />
                      )}
                    </>
                  </div>
                ))}
              </div>
            ) : (
              <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
            )}
          </div>
          <Grid lg={12} item container spacing={2}>
            <Grid item lg={this.state.uploadoption !== true ? 1.4 : 4} xs={12}></Grid>
            {this.state.uploadoption !== true && (
              <Grid item lg={3} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BROWSE')}`}
                  disabled={this.state.Browseenable == false}
                  onClick={() => {
                    this.setState({ ImageFolder: true });
                  }}
                />
              </Grid>
            )}
            {this.state.uploadoption !== true && (
              <Grid item lg={3} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  disabled={this.state.RemovedimageId.length === 0 && this.state.image.length === 0}
                  onClick={() => {
                    this.CallRemoveOrUpload();
                  }}
                />
              </Grid>
            )}
            <Grid item lg={3} xs={12}>
              <DefaultButton
                className="button"
                styles={PIMfooterButtonConfirm}
                text={`${this.props.t('BTN_CANCEL')}`}
                onClick={() => {
                  this.setState({ imgVisible: false, image: [], RemovedimageId: [] });
                  this.prevImage = [];
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

        <div className="QCTable1 dataTableFixedFooter">
          <DataTable
            className="datatable"
            columns={columns}
            data={this.state.PSTracking}
            onColumnOrderChange={handleColumnReorder}
            selectableRows={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            clearSelectedRows={this.state.noSelection}
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
const ComponentTranslated = withTranslation()(PhotoShootTracking);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
