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
  IDragOptions,
  ContextualMenu,
  TextField,
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
  PIMdropdownStylesDd,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHeaderButtons,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import DataTable from 'react-data-table-component';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import {
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
  iconButtonStyles123,
  iconButtonStyless,
  iconButtonStyless1,
  iconButtonStylesss,
  iconButtonStylesssClicked,
  iconButtonStylesssClicked1,
  rejectComment,
  rejectIcon,
} from '@/common/properties';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import { FaRegClone } from 'react-icons/fa';
import AuthManagerService from '@/services/AuthManagerService';
import { ImageTypeDropdown } from '@/services/ApiClient';

interface TableColumn<T> {
  key: string;
  name: JSX.Element;
  selector: (row: T) => any;
  isVisible: boolean;
  width?: string;
  minWidth?: string;
  Width?: string;
  position?: string;
}
class SHOOTTRACKING extends React.Component<any, any> {
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token: any = sessionStorage.getItem('token');
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  Imageoption: any = 'All';
  Content: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      contentRecord: [],
      currentRecord: [],
      searchData: [],
      isRecord: false,
      searchKey: '',
      approveVisible: false,
      selectedRowData: [],
      recjectVisible: false,
      otherSplitComment: '',
      imgVisible: false,
      imageLoading: false,
      refreshKey: 0,
      exportvisible: false,
      searchval: '',
      isDropdownOpen: null,
      selectedOption: '',
      PSstatusopt: null,
      itemcode: null,
      Picturedata: [],
      imageURLs: [] as string[],
      optionSelected: [],
      fullImageVisible: false,
      fullImageSrc: '',
      noSelection: false,
      selecteddept: [],
      divisionids: [],
      StockReturnToqc: [],
    };
  }

  commonDateRef = React.createRef<CommonDateRef>();

  async componentDidMount() {
    this.setState({
      isRecord: true,
      divisionids: [],
    });
    this.refresh(this.state.fromDate, this.state.toDate);

    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((dept) => {
      const matchingId = filteredDeptId?.find((id) => id === dept.id);
      const matchingDepartment = departmentList?.find((dept) => dept.id === matchingId);
      if (matchingDepartment) {
        departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
        deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
      }
    });
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
      deptOpt.push({ key: element.id, text: element.name });
    });
    this.setState({
      departmentOption: departmentOption,
      deptOption: deptOpt,
    });
    this.setState({ selectedOption: 'all', });
  }
  async refresh(startDate: any, endDate: any) {
    this.setState({
      isRecord: true,
      selectedRowData: [],
      selecteddept: [],
      divisionids: [],
    });
    this.Imageoption = 'All';
    const fromDate = startDate?.toLocaleDateString('en-CA') || '';
    const toDate = endDate?.toLocaleDateString('en-CA') || '';
    const imageid1 = this.state.optionSelected;
    setTimeout(async () => {
      const imageid = this.state.optionSelected;
      const response: any = (await this.apiClient.getContentRecords(fromDate, toDate, imageid)).result;
      const filteredResponse = response?.filter((item: any) => this.deptId.includes(item.divisionId));
      const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      const appConfiguration: any = await this.apiClient.getAppConfig();
      const StockReturnToqc = JSON.parse(
        appConfiguration?.result?.find((e: any) => e.name == 'StockReturnToqc')?.jsonValue,
      );
      var Photoshootstatus = (await this.apiClient.photoshootstatus())?.result;
      var PhotoSstatus: { key: number | undefined; text: string | undefined }[] = [];
      Photoshootstatus?.forEach((element) => {
        PhotoSstatus.push({ key: element.id, text: element.name });
      });
      var ImageTypeList = (await this.apiClient.getProductImageType())?.result;
      var ImageTypeOpt: { key: number | undefined; text: string | undefined }[] = [];
      ImageTypeList?.forEach((element) => {
        if (element.id === 4) {
          ImageTypeOpt.unshift({ key: element.id, text: element.name });
        } else {
          ImageTypeOpt.push({ key: element.id, text: element.name });
        }
      });
      var Picture: any = (await this.apiClient.getPictureCamQcL1())?.result;
      var defaultdata = ImageTypeOpt.find((e: any) => e.key == 4);
      this.setState({
        contentRecord: responeWithIndex,
        //isRecord: responeWithIndex.length > 0 ? true : false,
        isRecord: false,
        searchData: responeWithIndex,
        currentRecord: responeWithIndex,
        IMdropdownOptions: ImageTypeOpt,
        selectedOptionIm: defaultdata?.key,
        PSstatus: PhotoSstatus,
        noSelection: !this.state.noSelection,
        searchKey: '',
        imagetype: null,
        Picturedata: Picture,
        StockReturnToqc: StockReturnToqc,
      });
    }, 100);
  }
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  onSearchClick = async (startDate: any, endDate: any) => {
    const fromDate = startDate?.toLocaleDateString('en-CA') || '';
    const toDate = endDate?.toLocaleDateString('en-CA') || '';
    const response: any = (await this.apiClient.getContentRecords(fromDate, toDate, this.state.divisionids.join(','))).result;
    const filteredResponse = response?.filter((item: any) => this.deptId.includes(item.divisionId));
    const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    this.setState({
      contentRecord: responeWithIndex
    })
  }
  columnFilter = async () => {
    var finalData: any = [];
    if (this.Content.length != 0) {
      finalData = this.state.currentRecord.filter((element: any) => {
        return element.photoShootStatus == this.Content;
      });
    } else {
      finalData = this.state.currentRecord;
    }
    this.setState({
      dropDownSelectedRecord: finalData,
      contentRecord: finalData,
    });
  };
  Action = async () => {
    if (this.state.approveVisible) {
      this.setState({ approveVisible: false });
      const Approve = 'yes';
      const Itemcode = parseInt(this.state.itemcode, 10);
      await this.apiClient.updateContentstate(Itemcode, Approve, undefined);
      this.refresh(null, null);
    } else if (this.state.recjectVisible) {
      this.setState({ recjectVisible: false });
      const reject = 'no';
      const rejectComment = this.state.otherSplitComment;
      const Itemcode = parseInt(this.state.itemcode, 10);
      await this.apiClient.updateContentstate(Itemcode, reject, rejectComment);
      this.refresh(null, null);
    }
  };
  downloadFile = (startDate: any, endDate: any) => {
    // const imageType = this.state.imagetype;
    // let divisionId = this.state.divisionids.join(',');
    // console.log( this.state.divisionids.join(','))
    const fromDate = startDate?.toLocaleDateString('en-CA') || '';
    const toDate = endDate?.toLocaleDateString('en-CA') || '';
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.remove('loading-indicator');
    const token = this.token;
    let url = `${baseUrl}/api/Content/ContentExportExcel`;
    url += `?${Object.entries({ token })
      .map(([key, value]) => `${key}=${value}`)
      .join('&')}`;
    if (fromDate) {
      url += `&fromDate=${fromDate}`;
    }
    if (toDate) {
      url += `&toDate=${toDate}`;
    }
    if (this.state.divisionids) {
      url += `&imageType=${this.state.divisionids.join(',')}`;
    }

    axios({
      url: url,
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
        link.setAttribute('download', 'ProductTrackerExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

      })
      .catch((error) => console.error('Error downloading file:', error));

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

  closingFullImageModal = () => {
    this.setState({ fullImageVisible: false, fullImageSrc: '' });
  }

  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  columnFilter1 = async () => {
    var finalData: any = [];
    if (this.state.IMdropdownOptions.length != 0) {
      if (this.Imageoption != 'All') {
        finalData = this.state.currentRecord.filter((element: any) => {
          return element.image == this.Imageoption;
        });
      } else {
        finalData = this.state.currentRecord;
      }
      this.setState({
        contentRecord: finalData,
      });
    } else {
      this.setState({
        contentRecord: this.state.currentRecord,
      });
    }
  };

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { searchData } = this.state;
    var searchResult = searchData.filter((element: any) => {
      return element?.itemcode.toString().includes(newValue.trim());
    });
    this.setState({
      contentRecord: searchResult,
      searchKey: newValue,
    });
  }

  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  Filterdata = () => {
    if (this.state.selecteddept.length != 0) {
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

          this.setState({ contentRecord: record, Dropdownvalue: id });
        }
      } else {
        this.setState({ contentRecord: this.state.currentRecord });
      }
    } else {
      this.setState({ contentRecord: this.state.currentRecord });
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

  showNextImage = () => {
    this.setState((prevState: any) => {
      // Find the index of the current image
      const currentIndex = prevState?.imageURLs.indexOf(prevState?.fullImageSrc);

      // Calculate the next index
      let nextIndex = currentIndex + 1;
      if (nextIndex >= prevState.imageURLs.length) {
        nextIndex = 0; // Wrap around to the first image
      }

      // Update the state with the new image
      return {
        currentIndex: nextIndex,
        fullImageSrc: prevState.imageURLs[nextIndex],
      };
    });
  };

  showPreviousImage = () => {
    this.setState((prevState: any) => {
      const currentIndex = prevState?.imageURLs.indexOf(prevState?.fullImageSrc);
      let prevIndex = currentIndex - 1;
      if (prevIndex < 0) {
        prevIndex = prevState.imageURLs.length - 1;
      }
      return {
        currentIndex: prevIndex,
        fullImageSrc: prevState.imageURLs[prevIndex],
      };
    });
  };

  render() {
    const { t } = this.props;
    const { deptOption } = this.state;
    const { StockReturnToqc } = this.state; 
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.selecteddept.length === deptOption.length
        : false;
    const columns: TableColumn<any>[] = [
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
        selector: (row: any) => <span title={row.itemcode}>{row.itemcode}</span>,
        isVisible: true,
        width: '140px',
      },
      {
        key: 'eanCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        width: '140px',
      },
      {
        key: 'ps_image',
        name: <div className={columnHeader}>{`${this.props.t('COL_PS_IMAGE')}`}</div>,
        selector: (row: any) => {
          if (row.itemcode) {
            return (
              <>
                <Icon
                  iconName="PhotoCollection"
                  title={t('COL_PS_IMAGE')}
                  className="iconStyle"
                  onClick={() => {
                    this.Imagedisplay(row.pictureid);
                  }}
                />
              </>
            );
          } else {
            return null;
          }
        },
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
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
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'date_received',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => <span title={row.receivedDate}>{row.receivedDate}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'priority',
        name: <div className={columnHeader}>{`${this.props.t('PRIORITY')}`}</div>,
        selector: (row: any) => <span title={row.priority}>{row.priority}</span>,
        isVisible: true,
        width: '100px',
      },
      {
        key: 'studio_name',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_NAME')}`}</div>,
        selector: (row: any) => <span title={row.studioName}>{row.studioName}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'studio_location',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_LOCATION')}`}</div>,
        selector: (row: any) => <span title={row.studioLocation}>{row.studioLocation}</span>,
        isVisible: true,
        minWidth: '135px',
      },
      {
        key: 'photoshoot_status',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9} style={{ marginTop: '12px' }}>
              <div className={columnHeader}>{`${this.props.t('COL_PHOTOSHOOT_STATUS')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.Content = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    PSstatusopt: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                selectedKey={this.state.PSstatusopt}
                styles={columnFilter}
                style={{ marginLeft: -48, marginTop: 28 }}
              />
              <IconButton
                style={{ marginLeft: -30, marginTop: 16 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isDivision && (
                <IconButton
                  style={{ marginLeft: -10, marginTop: 16 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.Content = [];
                    this.setState({ isDivision: false, PSstatusopt: 0 });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        position: 'fixed',
        selector: (row: any) => {
          if (this.state.contentRecord.length !== 0) {
            if (row.photoShootStatus === 'Return') {
              return (
                <span className={`customer-badge status-Rejected`}>
                  <span title={`${row.photoShootStatus}`}>{row.photoShootStatus}</span>
                </span>
              );
            } else {
              if (row.photoShootStatus === 'Completed') {
                return (
                  <span className={`customer-badge status-Approved`}>
                    <span title={`${'Completed'}`}>{'Completed'}</span>
                    <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
                      {/* {row.photoshootComment != null ? (
                        <FaRegClone
                          onClick={() => {
                            this.setState({
                              recjected: true,
                              descriptionValue: row.photoshootComment,
                            });
                          }}
                        />
                      ) : (
                        ''
                      )} */}
                    </span>
                  </span>
                );
              } else {
                if (row.photoShootStatus === null) {
                  return <span></span>;
                } else {
                  return (
                    <span className={`customer-badge status-Pending`}>
                      <span title={`${'Pending'}`}>{'Pending'}</span>
                    </span>
                  );
                }
              }
            }
          }
        },
        isVisible: true,
        minWidth: '160px',
      },
      {
        key: 'Website_Live_date',
        name: <div className={columnHeader}>{`${this.props.t('WEBSITE_LIVE_DATE')}`}</div>,
        selector: (row: any) => <span title={row.date}>{row.date}</span>,
        isVisible: true,
        width: '135px',
      },
      {
        key: 'Range_Shot_date',
        name: <div className={columnHeader}>{`${this.props.t('RANGE_SHOT_DATE')}`}</div>,
        selector: (row: any) => <span title={row.rangeshotdate}>{row.rangeshotdate}</span>,
        isVisible: true,
        Width: '135px',
      },
      {
        key: 'Individual_Shot_date',
        name: <div className={columnHeader}>{`${this.props.t('INDIVIDUAL_SHOT_DATE')}`}</div>,
        selector: (row: any) => (
          <span title={row.individualstyledate}>{row.individualstyledate}</span>
        ),
        isVisible: true,
        Width: '135px',
      },
      {
        key: 'Catalogue_Shot_date',
        name: <div className={columnHeader}>{`${this.props.t('CATALOGUE_SHOT_DATE')}`}</div>,
        selector: (row: any) => <span title={row.catalogshotdate}>{row.catalogshotdate}</span>,
        isVisible: true,
        Width: '135px',
      },
      {
        key: 'stock_return_to_qc',
        name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN_TO_QC')}`}</div>,
        isVisible: true,
        minWidth: '160px',
        selector: (row: any) => {
          const text = StockReturnToqc.find((option: { key: number; }) => option.key === parseInt(row.stockreturn))?.text || '';
          switch (row.stockreturn) {
            case '0':
              return <span title={text}>{text}</span>;
            case '1':
              return (
                <>
                  <span title={text}>{text}</span>
                  <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
                    {row.photoshootComment != null ? (
                      <FaRegClone
                        onClick={() => {
                          this.setState({
                            recjected: true,
                            descriptionValue: row.photoshootComment,
                          });
                        }}
                      />
                    ) : (
                      ''
                    )}
                  </span>
                </>
              );
            }
          },
        },
    ]; //.filter((column) => column !== null);
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
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
                >{`${this.props.t('CONTENT_SHOOTTRACKING')}`}</Text>
              </Grid>
              <Grid item lg={0.88} xs={2} />
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyle"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={() => {
                    if (this.state.contentRecord.length !== 0) {
                      this.downloadFile(this.state.fromDate, this.state.toDate);
                    }
                  }}
                />
              </Grid>
            </Grid>
          </Stack>
          <Grid lg={12} item container spacing={0.8} direction={'row'}>
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
            {/* <Grid item lg={2} xs={6}>
              <Dropdown
                options={this.state.IMdropdownOptions}
                styles={PIMDropdownStyles}
                key={this.state.refreshKey}
                selectedKey={this.state.optionSelected}
                //defaultSelectedKey={4}
                //selectedKey={this.state.selectedOptionIm}
                placeholder={`${t('DROPDOWN_IMAGE_TYPE')}`}
                onChanged={(option) => {
                  this.setState({ selectedOptionIm: option.key, optionSelected: option.key, imagetype: option.text });
                  this.Imageoption = option.text;
                }}
              />
            </Grid> */}
            <Grid item lg={1.8} xs={6}>
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
                        .map((option: any) => (option.text === 'Select All' ? null : option.text))
                        .filter(Boolean);
                    const newSelectedSeasons1 = allSelected
                      ? []
                      : deptOption
                        .map((option: any) => (option.text === 'Select All' ? null : option.key))
                        .filter(Boolean);
                    this.setState(
                      {
                        selecteddept: newSelectedSeasons,
                        searchKey: '',
                        divisionSelected: '',
                        divisionids: newSelectedSeasons1,
                      },
                      // () => {
                      //   this.Filterdata();
                      // },
                    );
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.selecteddept)
                      ? [...this.state.selecteddept]
                      : [];
                    const updatedSelectedSeasons1 = Array.isArray(this.state.divisionids)
                      ? [...this.state.divisionids]
                      : [];
                    const isSelected = Array.isArray(this.state.selecteddept)
                      ? this.state.selecteddept.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                      const index1 = updatedSelectedSeasons1.indexOf(selectedOption.key);
                      updatedSelectedSeasons1.splice(index1, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                      updatedSelectedSeasons1.push(selectedOption.key);
                    }
                    this.setState(
                      {
                        selecteddept: updatedSelectedSeasons,
                        divisionids: updatedSelectedSeasons1,
                        divisionSelected: '',
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
            </Grid>
            <Grid item lg={2} xs={6}>
              <CommonDate
                onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                onToDateChange={(e: any) => this.handleToDateChange(e)}
                ref={this.commonDateRef}
                label={`${t('RECEIVED_DATE')}`}
              />
            </Grid>
            {/* <Grid item lg={1.88} xs={5.3}>
                <DefaultButton
                  className="button"
                  text={t('EDIT')}
                  styles={this.state.marketPlaceId != 1 ? PIMButtons : PIMHeaderButtons}
                />
              </Grid>
              <Grid item lg={1.88} xs={5.3}>
                <DefaultButton
                  className="button"
                  text={t('SUBMIT')}
                  styles={this.state.marketPlaceId != 1 ? PIMButtons : PIMHeaderButtons}
                />
              </Grid> */}
            <Grid item lg={4.77} xs={0} />
            <Grid item lg={0.5} xs={0.8}>
              <Icon
                iconName="Search"
                title={t('BTN_SEARCH')}
                className="iconStyle"
                onClick={() => {
                  if (
                    this.state.fromDate ||
                    this.state.toDate ||
                    (this.state.divisionids && this.state.divisionids.length != 0)
                  ) {
                    this.onSearchClick(this.state.fromDate, this.state.toDate);
                    // this.columnFilter();
                    // this.columnFilter1();
                    // this.Filterdata();
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
                    this.state.fromDate || this.state.toDate
                      ? true
                      : false ||
                      this.state.searchKey !== '' ||
                      this.state.optionSelected.length != 0 ||
                      this.state.selecteddept.length != 0
                  ) {
                    this.setState({
                      fromDate: null,
                      toDate: null,
                      optionSelected: [],
                      searchKey: '',
                      divisionids: [],
                      divisionSelected: '',
                    });
                    this.refresh(null, null);
                    this.ClearDates();
                  }
                  this.Imageoption = 'All';
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={0.8}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={async () => {
                  this.setState({
                    fromDate: null,
                    toDate: null,
                    optionSelected: [],
                    isDivision: null,
                  });
                  this.setState({ isRecord: true });
                  this.setState({ isRecord: false });
                  this.ClearDates();
                  this.refresh(null, null);
                }}
              />
            </Grid>
          </Grid>
          {/* <Modal
              isOpen={this.state.imgVisible}
              containerClassName={PIMcontentStyles.imageContainer}
              isBlocking={true}
              onDismiss={() =>
                this.setState({
                  imgVisible: false,
                })
              }
            >
              <div className={PIMcontentStyles.header}>
                <span className="apptext1">{t('IMAGE')}</span>
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => this.setState({ imgVisible: false })}
                />
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
            </Modal> */}
          <Modal
            isOpen={this.state.recjected}
            containerClassName={PIMcontentStyles.confirmContainer}
            onDismiss={() => this.setState({ recjected: false })}
            isBlocking={false}
          >
            {/* <div className={PIMcontentStyles.header}> */}
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
            isOpen={this.state.approveVisible}
            containerClassName={PIMcontentStyles.confirmContainer}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                approveVisible: false,
                row: [],
              })
            }
          >
            <div className={PIMcontentStyles.confirmbody}>
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
                      this.setState({ approveVisible: false });
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
                      this.setState({ approveVisible: false, row: [] });
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
            onDismiss={() =>
              this.setState({
                approveVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmbody2}>
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
            isOpen={this.state.recjectVisible}
            containerClassName={PIMcontentStyles.container}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                recjectVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <Text variant="xLarge" className="apptext3">{`${this.props.t(
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
            isOpen={this.state.imgVisible}
            containerClassName={PIMcontentStyles.imageContainerPhotoshote}
            // isBlocking={true}
            onDismiss={() => this.setState({ imgVisible: false })}
          >
            <div className={PIMcontentStyles.header}>
              <span className="apptext1">{t('IMAGE_UPLOAD')}</span>
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => this.setState({ imgVisible: false })}
              />
            </div>
            <div className={PIMcontentStyles.imagebody}>
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
          <div className="table1 dataTableFixedFooter">
            <DataTable
              className="datatable"
              columns={columns}
              data={this.state.contentRecord}
              onColumnOrderChange={handleColumnReorder}
              selectableRows={this.state.selectedRowData}
              clearSelectedRows={this.state.noSelection}
              pagination={true}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeader
              striped
              fixedHeaderScrollHeight="68vh"
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
const ComponentTranslated = withTranslation()(SHOOTTRACKING);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
