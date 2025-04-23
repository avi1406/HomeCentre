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
  TextField,
  ContextualMenu,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { DocumentCheckmarkFilled } from '@fluentui/react-icons';
import { Grid } from '@mui/material';
import {
  columnHeader,
  customModalStyles,
  PIMButtons,
  PIMcontentStyles,
  PIMDropdownStyles,
  PIMDropdownStyles1,
  PIMDropdownStyles11,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHeaderButtons,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import {
  approveIcon,
  cancelIcon,
  clearFilter,
  ChevronLeft,
  ChevronRight,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  iconButtonStyles1,
  iconButtonStyles12,
  iconButtonStyles123,
  iconButtonStyless,
  iconButtonStyless1,
  iconButtonStylessClicked,
  iconButtonStylesss,
  iconButtonStylesssClicked,
  iconButtonStylesssClicked1,
  rejectComment,
  rejectIcon,
  rejectReasonDropdown,
  UploadStyles,
} from '@/common/properties';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import { downloadFileCommon } from '@/pages/commonFunctions';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import { ImageTypeDropdown, ProductImageModel, TasklevelSequence } from '@/services/ApiClient';
import DataTable from 'react-data-table-component';
import { FaRegClone } from 'react-icons/fa';
import AuthManagerService from '@/services/AuthManagerService';

class CamQCL2 extends React.Component<any, any> {
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token: any = sessionStorage.getItem('token');
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  username: any = this.user?.name || '';
  Divisiton: any = [];
  Imageoption: any = 'All';
  Handover1: any = [];
  PTstatus: any = [];
  constructor(props: any) {
    super(props);
    this.state = {
      CamQCL2: [],
      searchData: [],
      isRecord: false,
      searchKey: '',
      pictureData: [],
      selectedRowData: [],
      imageURL: [],
      fromDate: null,
      isItClicked: false,
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
      exportvisible: false,
      searchval: '',
      isDropdownOpen: false,
      selectedOption: '',
      isMouseOverDropdown: false,
      dynamicDropdownOptions: [],
      isItDrape: true,
      isItPhysical: true,
      IMdropdownOptions: [],
      optionSelected: [],
      selectedOptionIm: null,
      selectedOptionLMop: null,
      QualitycheckIM: null,
      Qualitycheck: [],
      QualitycheckM: false,
      itemcode: null,
      QualitycheckIMVal: '',
      otherSplitComment: '',
      divisionSelected: null,
      isDivision: false,
      HandOverStatus: null,
      CAMQCProcessStatus: [],
      photoShootOption: [],
      Picturedata: [],
      imageURLs: [] as string[],
      Photostatus: [],
      camQcSubmit: false,
      fullImageVisible: false,
      fullImageSrc: '',
      A_D_Visible1: false,
    };
  }

  commonDateRef = React.createRef<CommonDateRef>();

  async componentDidMount() {
    this.setState({
      isRecord: true,
    });
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
      deptOpt.push({ key: element.id, text: element.name });
    });
    const rejectComment = (await this.apiClient.getRejectReason()).result;
    var comment: any = [];
    rejectComment?.forEach((element) => {
      comment.push({ key: element.name, text: element.name });
    });
    this.setState({
      departmentOption: departmentOption,
      rejectCommentList: comment,
    });
    this.refresh(null, null, null);
  }
  async refresh(startDate: any, endDate: any, optionSelected: any) {
    this.setState(
      {
        isRecord: true,
        isItPhysical: true,
        isItDrape: true,
        searchKey: '',
        selectedRowData: [],
        isDivision: false,
        divisionSelected: null,
        Divisiton: [],
        HandoverStatus: null,
        isHandOver: false,
        noSelection: !this.state.noSelection,
        HandOver: null,
        PSstatusopt: '',
      },
      () => {
        this.setState({ selectedRowData: [] });
      },
    );
    const picData = (await this.apiClient.getPSFileNameAndUrl()).result;
    this.Imageoption = 'All';
    const fromDate = startDate?.toLocaleDateString('en-CA') || '';
    const toDate = endDate?.toLocaleDateString('en-CA') || '';
    const imageid1 = this.state.optionSelected;
    setTimeout(async () => {
      const imageid = this.state.optionSelected;

      var response: any = (await this.apiClient.getCamQcL2details(fromDate, toDate, imageid))
        ?.result;
      const filteredResponse = response?.filter((item: any) =>
        this.deptId.includes(item.divisionId),
      );
      const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      var PhotoshootCAM = (await this.apiClient.photoshootstatus())?.result;
      var PhotoSstatus: { key: number | undefined; text: string | undefined }[] = [];
      PhotoshootCAM?.forEach((element: any) => {
        PhotoSstatus.push({ key: element.id, text: element.name });
      });
      var data: any = [];
      var status: any = [];
      const appConfiguration: any = await this.apiClient.getAppConfig(); //getHandOverStatus
      const HnadoverStatus: any = (await this.apiClient.getHandOverStatus())?.result;
      status = (await this.apiClient.getItemStatus())?.result;
      this.setState({
        Photostatus: status,
        PSstatus: PhotoSstatus,
      });

      // const HnadoverStatus = JSON.parse(
      //   appConfiguration?.result?.find((e: any) => e.name == 'HandoverStatus')?.jsonValue,
      // );
      var hanDOverOpt: { key: number | undefined; text: string | undefined }[] = [];
      HnadoverStatus?.forEach((element: any) => {
        hanDOverOpt.push({ key: element.id, text: element.name });
      });
      var Phstatus: { key: number | undefined; text: string | undefined }[] = [];
      status?.forEach((element: any) => {
        Phstatus.push({ key: element.itemCode, text: element.percentageOfKeysInLabels });
      });
      responeWithIndex?.forEach((row: any) => {
        let handoverStatus1;
        let Photostetuss;
        if (row.itemCode != null) {
          const pstatus = status
            ? Phstatus.find((element: any) => element.key == row.itemCode)
            : null;
          if (pstatus) {
            Photostetuss = pstatus.text;
          }
        }
        const statusKey = parseInt(row.handoverStatus, 10);
        if (row.handoverStatus != null) {
          const statusObject = hanDOverOpt
            ? hanDOverOpt.find((element: any) => element.key === statusKey)
            : null;
          if (statusObject) {
            handoverStatus1 = statusObject.text;
          }
        }
        const newData = {
          ...row,
          handoverStatus1: handoverStatus1,
          phstatus: Photostetuss,
        };
        data.push(newData);
      });
      var Picture: any = (await this.apiClient.getPictureCamQcL1())?.result;
      var photoShootList = (await this.apiClient.getPhotoShootStatus())?.result;
      var photoshoOpt: { key: number | undefined; text: string | undefined }[] = [];
      photoShootList?.forEach((element) => {
        photoshoOpt.push({ key: element.id, text: element.name });
      });

      const QualityCheck = JSON.parse(
        appConfiguration?.result?.find((e: any) => e.name == 'QualityCheck')?.jsonValue,
      );

      var ImageTypeList = (await this.apiClient.getProductImageType())?.result;
      var ImageTypeOpt: { key: number | undefined; text: string | undefined }[] = [];
      ImageTypeList?.forEach((element) => {
        if (element.id === 4) {
          ImageTypeOpt.unshift({ key: element.id, text: element.name });
        } else {
          ImageTypeOpt.push({ key: element.id, text: element.name });
        }
      });
      var ProcessStatus = (await this.apiClient.cAMQCProcessStatus())?.result;
      var defaultdata = ImageTypeOpt.find((e: any) => e.key == 4);
      this.setState({
        isRecord: false,
        IMdropdownOptions: ImageTypeOpt,
        selectedOptionIm: defaultdata?.key,
        CamQCL2: data,
        searchData: data,
        Qualitycheck: QualityCheck,
        HandOverStatus: hanDOverOpt,
        CAMQCProcessStatus: ProcessStatus,
        photoShootOption: photoshoOpt,
        Picturedata: picData,
      });
    }, 100);
  }

  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  Action = async () => {
    let response: any;
    const rejectComment = this.state.otherSplitComment;
    if (this.state.approveVisible) {
      this.setState({ approveVisible: false });
      const Approve = 'yes';
      const Itemcode = parseInt(this.state.itemcode, 10);
      response = await this.apiClient.cAMQCActionState(Itemcode, Approve, undefined, this.username);
    } else if (this.state.QualitycheckMfail) {
      this.setState({ QualitycheckMfail: false });
      const reject = 'CamQCL2';
      const Itemcode = parseInt(this.state.itemcode, 10);
      response = await this.apiClient.rejectedItemcodeCAML2(
        Itemcode,
        this.username,
        rejectComment,
        reject,
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
  downloadFile = (fromDate: any, toDate: any, imageType: any) => {
    var body = new ImageTypeDropdown();
    body.fromDate = this.state.fromDate?.toLocaleDateString('en-CA');
    body.toDate = this.state.toDate?.toLocaleDateString('en-CA');
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
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/CamQc/CAMQCL2ExportExcel`,
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
        link.setAttribute('download', 'ShootReviewerExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };

  closingImageModal = () => {
    this.setState({ imgVisible: false, imageURLs: [] });
  };
  closingFullImageModal = () => {
    this.setState({ fullImageVisible: false, fullImageSrc: '' });
  };

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: [] }, () => {
      this.setState({ selectedRowData: e.selectedRows });
    });
  };
  Quality = async () => {
    this.setState({ QualitycheckM: false, QualitycheckIM: null });
    if (this.state.itemcode != null) {
      const Itemcode = parseInt(this.state.itemcode, 10);
      const check = 'Pass';
      await this.apiClient.updateCAMQCQualityCheck(Itemcode, check, this.username);
      this.refresh(null, null, null);
    }
  };
  showNextImage = () => {
    this.setState((prevState: any) => {
      const currentIndex = prevState.imageURL.indexOf(prevState.fullImageSrc);
      let nextIndex = currentIndex + 1;
      if (nextIndex >= prevState.imageURL.length) {
        nextIndex = 0;
      }
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
  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date, downloadFromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date, downloadToDate: date });
  };

  // columnFilter = async () => {
  //   var finalData: any = [];
  //   if (this.state.IMdropdownOptions.length != 0) {
  //     if (this.Imageoption != 'All') {
  //       finalData = this.state.searchData.filter((element: any) => {
  //         return element.imageType == this.Imageoption;
  //       });
  //     } else {
  //       finalData = this.state.searchData;
  //     }
  //     this.setState({
  //       CamQCL2: finalData,
  //     });
  //   } else {
  //     this.setState({
  //       CamQCL2: this.state.searchData,
  //     });
  //   }
  // };

  mergedColumnFilters = async () => {
    var finalData: any = [];
    if (this.Divisiton.length != 0) {
      finalData = this.state.searchData.filter((element: any) => {
        return element.divisionId == this.Divisiton;
      });
    } else {
      finalData = this.state.searchData;
    }

    if (this.state.IMdropdownOptions.length !== 0 && this.state.selectedOptionIm !== 4) {
      finalData = finalData.filter((element: any) => element.imageType === this.Imageoption);
    }

    // if (this.PTstatus.length != 0) {
    //   if (this.PTstatus == 'Pending' || this.PTstatus == '1') {
    //     finalData = finalData.filter((element: any) => {
    //       return element.photoShootStatus == 'Pending';
    //     });
    //   } else {
    //     finalData = finalData.filter((element: any) => {
    //       return element.photoShootStatus == this.PTstatus;
    //     });
    //   }
    // }

    if (this.PTstatus.length != 0) {
      if (this.PTstatus === 'Pending' || this.PTstatus === '1') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === 'Pending');
      } else if (this.PTstatus === 'Completed' || this.PTstatus === '2') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === 'Completed');
      } else if (this.PTstatus === 'Closed' || this.PTstatus === '3') {
        finalData = finalData.filter((element: any) => element.photoShootStatus === 'Closed');
      } else {
        finalData = finalData.filter((element: any) => element.photoShootStatus === this.PTstatus);
      }
    }

    if (this.Handover1.length != 0) {
      finalData = finalData.filter((element: any) => {
        return element.handoverStatus == this.Handover1;
      });
    }

    this.setState({
      CamQCL2: finalData,
    });
  };

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var searchResult = this.state.searchData.filter((element: any) => {
      return element?.itemCode.toString().includes(newValue.trim());
    });
    this.setState({
      CamQCL2: searchResult,
      searchKey: newValue,
    });
  }

  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  // dragOptions: IDragOptions = {
  //   moveMenuItemText: 'Move',
  //   closeMenuItemText: 'Close',
  //   menu: ContextualMenu,
  //   dragHandleSelector: '.ms-Modal-scrollableContent > div:first-child',
  // };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  containerStyle = mergeStyles({
    width: '50px',
    height: '20px',
    border: '1px solid #ccc',
    borderRadius: '4px',
    overflow: 'hidden',
  });

  fillStyle = (percentage: number) => {
    let color = '#0078D4';
    if (percentage >= 1 && percentage <= 29) {
      color = 'red';
    } else if (percentage >= 30 && percentage <= 59) {
      color = 'yellow';
    } else if (percentage >= 60 && percentage <= 100) {
      color = 'green';
    }

    return mergeStyles({
      height: '100%',
      width: `${percentage}%`,
      backgroundColor: color,
    });
  };

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
        imageLoading,
      });
    }
  };

  FluentProgressBar: React.FC<{ code: number }> = ({ code }) => {
    return (
      <Stack verticalAlign="center" verticalFill tokens={{ childrenGap: 8 }}>
        <Text>{`${code}%`}</Text>
        <div className={this.containerStyle}>
          <div className={this.fillStyle(code)}></div>
        </div>
      </Stack>
    );
  };

  handleCamQcSubmit = () => {
    if (this.state.camQcSubmit == true) {
      this.CamQcSubmit();
    }
  };

  Camsubmit = () => {
    this.setState({ A_D_Visible1: false });
    if (this.state.selectedRowData.length !== 0) {
      const l2qualitycheck = this.state.selectedRowData.some((e: any) => {
        return e.l2qualitycheck != 'Pass';
      });
      const phstatus = this.state.selectedRowData.some((e: any) => {
        return e.phstatus != 100;
      });
      const submit = this.state.selectedRowData.some((e: any) => e.camqcl2submit == 1);
      if (l2qualitycheck && phstatus) {
        this.notify.showErrorNotify(this.props.t('ITEMCODE_NOT_ABLE_TO_SUBMIT'));
      }
      // else if (phstatus && l2qualitycheck) {
      //   this.notify.showErrorNotify(this.props.t('QUALITY_PROGRESS'));
      // }
      else if (submit) {
        this.notify.showErrorNotify(this.props.t('ITEMCODE_SUBMITED'));
      } else if (l2qualitycheck) {
        this.notify.showErrorNotify(this.props.t('DO_QUALITY_CHECK'));
      } else if (phstatus) {
        this.notify.showErrorNotify(this.props.t('PROGRESS_NOT_COMPLETED'));
      }
      if (submit == false && phstatus == false && l2qualitycheck == false) {
        this.setState(
          {
            camQcSubmit: true,
          },
          () => {
            this.handleCamQcSubmit();
          },
        );
      }
      return submit || phstatus || l2qualitycheck;
    }
  };

  CamQcSubmit = async () => {
    this.setState({
      camQcSubmit: false,
    });
    const { selectedRowData } = this.state;
    if (selectedRowData.length != 0) {
      const itemCodes = selectedRowData.map((element: any) => element.itemCode);
      try {
        await this.apiClient.cAQCL2Submit(itemCodes, '1', this.username).then((e: any) => {
          this.notify.showSuccessNotify(this.props.t('SUBMITTED_SUCCESSFULLY'));
        });
        this.refresh(null, null, null);
      } catch (error: any) {
        this.notify.showErrorNotify(error);
      }
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
      },
      {
        key: 'eancode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        width: '140px',
      },
      {
        key: 'qualityCheck',
        name: <div className={columnHeader}>{`${this.props.t('COL_QUALITY_CHECK')}`}</div>,
        selector: (row: any) => {
          if (this.state.CamQCL2.length !== 0) {
            let photoShootStatus = 'Pending';
            const statusKey = parseInt(row.photoShootStatus, 10);
            if (row.photoShootStatus != null) {
              const statusObject = this.state.photoShootOption
                ? this.state.photoShootOption.find((element: any) => element.key === statusKey)
                : null;
              if (statusObject) {
                photoShootStatus = statusObject.text;
              }
            }
            return (
              <>
                <Grid container direction={'row'} spacing={2}>
                  {(row.l2qualitycheck == 'Pass' || row.l2qualitycheck == null) && (
                    <Grid item>
                      <IconButton
                        styles={
                          row.l2qualitycheck !== null
                            ? row.l2qualitycheck == 'Pass'
                              ? iconButtonStylesssClicked
                              : iconButtonStyless
                            : iconButtonStylesssClicked1
                        }
                        iconProps={approveIcon}
                        onClick={() => {
                          if (row.l2qualitycheck == null) {
                            this.setState({
                              itemcode: row.itemCode,
                              QualitycheckM: true,
                              QualitycheckMfail: false,
                            });
                          }
                        }}
                      />
                    </Grid>
                  )}
                  {(row.l2qualitycheck == 'Fail' || row.l2qualitycheck == null) && (
                    <Grid item>
                      <IconButton
                        styles={
                          row.l2qualitycheck !== null
                            ? row.l2qualitycheck == 'Fail'
                              ? iconButtonStyless1
                              : iconButtonStylesssClicked
                            : iconButtonStyless
                        }
                        iconProps={rejectIcon}
                        disabled={row.imageType == 'GRN'}
                        onClick={() => {
                          if (row.l2qualitycheck == null) {
                            this.setState({
                              itemcode: row.itemCode,
                              QualitycheckMfail: true,
                              QualitycheckM: false,
                            });
                          }
                        }}
                      />
                    </Grid>
                  )}
                </Grid>
              </>
            );
          }
        },
        isVisible: true,
        width: '150px',
      },
      {
        key: 'psimage',
        name: <div className={columnHeader}>{`${this.props.t('PS_IMAGE')}`}</div>,
        selector: (row: any) => {
          if (row.itemCode) {
            let ImgUrl: string[] = []; // Ensure ImgUrl is initialized as an array
            if (row.pictureid) {
              const pictureIdsArray = row.pictureid
                .split(',')
                .map((id: any) => parseInt(id.trim(), 10));
      
              const matchingObjects = this.state.Picturedata.filter((e: any) =>
                pictureIdsArray.includes(e.id),
              );
              
              ImgUrl = matchingObjects.map((obj: any) => obj.imageUrl.split('?')[0]);
            }
      
            // Only show the icon if ImgUrl contains at least one valid URL
            if (ImgUrl.length > 0) {
              return (
                <Icon
                  iconName="PhotoCollection"
                  title={t('COL_PS_IMAGE')}
                  className="iconStyle"
                  onClick={() => {
                    this.setState({
                      imgVisible: true,
                      itemcode: row.itemCode,
                      imageURL: ImgUrl, // Store the array of URLs in state
                      imageLoading: false,
                    });
                  }}
                />
              );
            }
          }
          return null;
        },
        isVisible: true,
        width: '100px',
      },        
      {
        key: 'progressStatus',
        name: <div className={columnHeader}>{`${this.props.t('COL_PROGRESS_STATUS')}`}</div>,
        cell: (row: any) => {
          if (this.state.CamQCL2.length !== 0) {
            return <this.FluentProgressBar code={row.phstatus} />;
          }
        },
        isVisible: true,
        minWidth: '140px',
      },

      {
        key: 'division',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>
            </Grid>
            <Grid item lg={1.3} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.departmentOption}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    searchKey: '',
                  });
                  this.Divisiton = selectedOption.key;
                  this.mergedColumnFilters();
                }}
                selectedKey={this.state.divisionSelected}
                styles={columnFilter}
                style={{ marginLeft: -2 }}
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
              {this.state.divisionSelected && (
                <IconButton
                  style={{ marginLeft: 2 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.Divisiton = [];
                    this.setState({ isDivision: false, divisionSelected: '' });
                    this.mergedColumnFilters();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        // position: 'fixed',
        selector: (row: any) => {
          if (this.state.CamQCL2.length !== 0) {
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
        isVisible: true,
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
        selector: (row: any) => <span title={row.name}>{row.name}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'studio_location',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_LOCATION')}`}</div>,
        selector: (row: any) => {
          const city = row.city || '';
          return typeof city === 'string' ? <span title={city}>{city}</span> : city;
        },
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'studio_capacity',
        name: <div className={columnHeader}>{`${this.props.t('CONTENT_STUDIO_CAPACITY')}`}</div>,
        selector: (row: any) => <span title={row.capacity}>{row.capacity}</span>,
        isVisible: true,
        minWidth: '135px',
      },
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
        selector: (row: any) => {
          if (row.imageType) {
            return (
              <>
                <span title={row.imageType}>{row.imageType}</span>
              </>
            );
          }
        },
        isVisible: true,
        width: '140px',
      },
      {
        key: 'priority',
        name: <div className={columnHeader}>{`${this.props.t('PRIORITY')}`}</div>,
        selector: (row: any) => {
          if (row.priority) {
            return (
              <>
                <span title={row.priority}>{row.priority}</span>
              </>
            );
          }
        },
        isVisible: true,
        width: '100px',
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
                options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.PTstatus = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    PSstatusopt: selectedOption.key,
                    searchKey: '',
                  });
                  this.mergedColumnFilters();
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
                    this.mergedColumnFilters();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          if (this.state.CamQCL2.length !== 0) {
            switch (row.photoShootStatus) {
              case 'Rejected':
                return (
                  <span className={`customer-badge status-Rejected`}>
                    <span title={`${'Rejected'}`}>{'Rejected'}</span>
                  </span>
                );
              case 'Completed':
                return (
                  <span className={`customer-badge status-Approved`}>
                    <span title={`${'Completed'}`}>{'Completed'}</span>
                  </span>
                );
              case 'Closed':
                return (
                  <span className={`customer-badge status-Closed`}>
                    <span title={`${'Closed'}`}>{'Closed'}</span>
                  </span>
                );
              case 'Pending':
                return (
                  <span className={'customer-badge status-Pending'}>
                    <span title={`${'Pending'}`}>{'Pending'}</span>
                  </span>
                );
              default:
                return <span></span>;
            }
          }
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'stock_return_to_qc',
        name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        selector: (row: any) => {
          switch (row.stockReturnToQC) {
            case '0':
              return <span title={`${'Yet To Return'}`}>{'Yet To Return'}</span>;
            case '1':
              return (
                <>
                  <span title={`${'Returned'}`}>{'Returned'}</span>
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
            default:
              return <span></span>;
          }
        },
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
                >{`${this.props.t('PHOTOSHOOT_IMAGE_QC')}`}</Text> */}
            </Grid>
            <Grid item lg={1} xs={2} />
            <Grid item lg={0.4} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.CamQCL2.length !== 0) {
                    this.downloadFile(this.state.fromDate, this.state.toDate, this.state.imageType);
                  }
                }}
              />
            </Grid>
          </Grid>
        </Stack>
        <Grid lg={12} item container spacing={0.8} direction={'row'}>
          <Grid item lg={0.1} xs={2} />
          <Grid item lg={1.92} xs={12}>
            <SearchBox
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ITEMCODE')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
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
              // selectedKey={this.state.selectedOptionIm}
              onChanged={(option) => {
                this.setState({ selectedOptionIm: option.key, optionSelected: option.key });
                this.Imageoption = option.text;
                // this.mergedColumnFilters();
              }}
            />
          </Grid>
          <Grid item lg={2} xs={5.3}>
            <CommonDate
              onMinDateChange={(e: any) => this.handleMinDateChange(e)}
              onToDateChange={(e: any) => this.handleToDateChange(e)}
              ref={this.commonDateRef}
              label={`${t('RECEIVED_DATE')}`}
            />
          </Grid>
          <Grid item lg={2} xs={6}>
            {/* <Icon
              iconName="WaitlistConfirm"
              className="iconStyle"
              title={this.props.t('ADD_BTN_SUBMIT')}
              onClick={() => {
                this.Camsubmit();
              }}
              // style={this.Camsubmit() ? { pointerEvents: 'none', opacity: 0.5 } : {}}
            /> */}
            <span title={this.props.t('ADD_BTN_SUBMIT')}>
              <DocumentCheckmarkFilled
                className="submitIconStyle"
                onClick={() => {
                  this.setState({ A_D_Visible1: true });
                }}
              />
            </span>
          </Grid>
          <Grid item lg={2.88} xs={1.8}></Grid>
          <Grid item lg={0.5} xs={1.8}>
            <Icon
              iconName="Search"
              title={t('BTN_SEARCH')}
              className="iconStyle"
              onClick={() => {
                if (this.state.fromDate || this.state.toDate) {
                  this.refresh(this.state.fromDate, this.state.toDate, this.state.optionSelected);
                } else {
                  this.mergedColumnFilters();
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
                    selectedRowData: [],
                    noSelection: !this.state.noSelection,
                  });
                  this.refresh(null, null, null);
                  this.ClearDates();
                }
                this.Imageoption = 'All';
              }}
            />
          </Grid>
          <Grid item lg={0.4} xs={1.8}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={async () => {
                this.setState({
                  isRecord: true,
                  fromDate: null,
                  toDate: null,
                  noSelection: !this.state.noSelection,
                  optionSelected: [],
                });
                await this.refresh(null, null, null);
                this.setState({ isRecord: false, isDivision: false, isHandover: false });
                this.ClearDates();
                this.Imageoption = 'All';
              }}
            />
          </Grid>
        </Grid>
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
              'QUALITY_CHECK_FAIL',
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
            <Text variant="xLarge" className="apptext2 dlttxt camtxt">{`${this.props.t(
              'QUALITY_CHECK_PASS',
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
          isOpen={this.state.approveVisible}
          containerClassName={PIMcontentStyles.confirmContainer}
          isBlocking={false}
          //dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              approveVisible: false,
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible: false, row: [] })}
          />
          <div className={PIMcontentStyles.confirmbody3}>
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
          isOpen={this.state.A_D_Visible1}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              A_D_Visible1: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
            <IconButton
              styles={UploadStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ A_D_Visible1: false });
              }}
            />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="apptext2">{`${t(
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
                  text={`${t('BTN_CONFIRM')}`}
                  onClick={this.Camsubmit.bind(this)}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ A_D_Visible1: false });
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
            data={this.state.CamQCL2}
            // selectableRowsVisibleOnly={this.state.selectedRowData}
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
const ComponentTranslated = withTranslation()(CamQCL2);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
