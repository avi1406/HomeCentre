import {
  DefaultButton,
  DocumentCard,
  Icon,
  IconButton,
  Modal,
  ProgressIndicator,
  Stack,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import { withTranslation } from 'react-i18next';
import {
  columnHeader,
  PIMButtons,
  PIMButtons1,
  PIMcontentStyles,
  PIMfileUpload,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMRemoveButton,
  removeIcon,
} from '../PimStyles';
import DataTable from 'react-data-table-component';
import UploadData from './UploadData';
import { Link } from 'react-router-dom';
import { Grid } from '@mui/material';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { Template, UploadPicture, UploadedFile, UploadedFileStatus } from '@/services/ApiClient';
import { BlockBlobClient } from '@azure/storage-blob';
import NotificationManager from '@/services/NotificationManager';
import {
  CancelButtonStyles,
  CancelStyles,
  UploadStyles,
  UploadimageStyles,
  cancelIcon,
  iconButtonStyles,
} from '@/common/properties';
import { baseUrl } from '@/services/Constants';
import { downloadFileCommon, downloadFileCommontemplate } from '@/pages/commonFunctions';
import UploadImageFolder from './UploadPicture';
import PsUploadImageFolder from './PsuploadPictureFolder';

class Data extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  token: any = sessionStorage.getItem('token');
  uploadedFileStatusId: null | undefined;
  constructor(props: any) {
    super(props);
    this.state = {
      fileData: '',
      divisionOption: [],
      visible: false,
      isRecord: false,
      tempalteOptions: [],
      selectedTemplateId: 0,
      percentageComplete: '',
      ProgressVisible: false,
      isNew: true,
      selectedRowUploadFile: new UploadedFile(),
      confirmModel: false,
      getUploadedFileStatus: [],
      isTemplateFile: false,
      publishData: [],
      selectedTemplate: '',
      rowData: [],
      uploadTemplateName: [],
      progressPercent: 0,
      imageProgress: false,
      Imagetext: '',
      ImagePercent: 0,
      setVisible: false,
      setVisible1: false,
      images: [],
      uploadedImages: [],
      uploadedFileStatusId: null,
      getClick: sessionStorage.getItem('Tableclicked') === 'true',
      droppedFiles: [],
    };
  }

  componentDidMount() {
    this.getUploadedFileStatus().then((r) => {
      this.refresh();
    });
  }
  componentDidUpdate(prevProps: Readonly<any>, prevState: Readonly<any>, snapshot?: any): void {
    const getClick = sessionStorage.getItem('Tableclicked');
    if (this.state.getClick && !this.state.isTemplateFile) {
      this.setState({ isTemplateFile: true });
    } else if (!this.state.getClick && this.state.isTemplateFile) {
      this.setState({ isTemplateFile: false }, () => {});
    }
    if (getClick == 'false' || getClick == null) {
      this.uploadtemplatestatus();
    }
  }

  async refresh() {
    this.setState({ isRecord: true });
    var division: any = [];
    var userdata: any = this.authManager.getUserData()?.roleId;
    var divisionId: any = this.authManager.getUserData()?.departmentId;
    const divisions = await this.apiClient.getDivision();
    divisions?.result?.forEach((element: any) => {
      division.push({ key: element.id, text: element.name });
    });
    var filteredDivision = division?.filter((o1: any) =>
      divisionId.some((o2: any) => o1.key === o2),
    );
    this.apiClient.getTemplates().then((r) => {
      const templateList: {
        key: number | undefined;
        text: string | undefined;
        id: number | undefined;
      }[] = [];
      r?.result?.forEach((element: Template) => {
        templateList.push({
          key: element?.divisionId,
          text: element?.name,
          id: element?.id,
        });
      });
      var filteredTemplate = templateList?.filter((o1: any) =>
        filteredDivision?.some((o2: any) => o1.key === o2.key),
      );
      // const [images, setImages] = useState(Array<File>());
      // const [visible, setVisible] = useState(false);
      const seenItems = new Set();
      const filteredTemplate1 = templateList?.filter((item) => {
        if (filteredDivision?.some((division: any) => item.key === division.key)) {
          const serializedItem = JSON.stringify(item);
          if (!seenItems.has(serializedItem)) {
            seenItems.add(serializedItem);
            return true;
          }
        }
        return false;
      });

      // const seenItemsArray = Array.from(seenItems).map((serializedItem: any) =>
      //   JSON.parse(serializedItem),
      // );

      // const sortedFiles = this.customSortingFunction(seenItemsArray, 'uid', 'buyer', 'qc');

      // this.listFiles(this.state.selectedTemplateId);
      this.listFiles(0, () => {
        // Handle the uploadedFileStatusId here
        if (this.state.uploadedFileStatusId === 5 || this.state.uploadedFileStatusId === 3) {
          // this.notify.showSuccessNotify(this.props.t('validated successfully'));
        } else {
          // this.notify.showErrorNotify(this.props.t('validated Failed'));
        }
        // this.setState({ isTemplateFile: true });
      });
      this.setState({
        tempalteOptions: filteredTemplate1.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
        isRecord: false,
        divisionOption: filteredDivision,
        isTemplateFile: false,
      });
      // this.setState({
      //   tempalteOptions: sortedFiles,
      // });
    });
  }
  // customSortingFunction(files: any, keyword1: any, keyword2: any, keyword3: any) {
  //   const keyword1Files = [];
  //   const keyword2Files = [];
  //   const keyword3Files = [];
  //   const otherFiles = [];
  //   for (const fileStr of files) {
  //     const file = fileStr.text.toString().toLowerCase();
  //     if (file.includes(keyword1)) {
  //       keyword1Files.push(fileStr);
  //     } else if (file.includes(keyword2)) {
  //       keyword2Files.push(fileStr);
  //     } else if (file.includes(keyword3)) {
  //       keyword3Files.push(fileStr);
  //     } else {
  //       otherFiles.push(fileStr);
  //     }
  //   }
  //   const compareById = (a: any, b: any) => {
  //     if (a.id === b.id) return 0;
  //     return a.id > b.id ? 1 : -1;
  //   };
  //   keyword1Files.sort(compareById);
  //   keyword2Files.sort(compareById);
  //   keyword3Files.sort(compareById);
  //   otherFiles.sort(compareById);
  //   return keyword1Files.concat(keyword2Files, keyword3Files, otherFiles);
  // }
  async getUploadedFileStatus() {
    this.apiClient.getUploadedFileStatus().then((r) => {
      this.setState({ getUploadedFileStatus: !r?.hasError ? r?.result : [] });
    });
  }
  handleIconButtonClick = () => {
    const currentClickValue = this.state.getClick;
    const newClickValue = currentClickValue === true ? false : true;
    if (currentClickValue !== newClickValue) {
      this.setState({ getClick: newClickValue === true });
    }
  };

  async listFiles(id: any, callback: () => void) {
    this.setState({ isRecord: true });
    const fileDatas: any = [];
    await this.apiClient.searchUploadedFiles('', 0, 0, id).then(async (r: any) => {
      const fileId = r?.result?.records[0]?.id;
      const status = (await this.apiClient.getInputTemplateData(fileId))?.result;
      const status1 = status?.filter((e) => e.tableName !== null);
      if (fileId !== undefined && status1?.length == 0) {
        this.apiClient.getUploadedFile(fileId).then((response: any) => {
          const uploadedFile = response?.result;
          const uploadedFileStatusId = uploadedFile ? uploadedFile.uploadedFileStatusId : '';
          fileDatas.push(response?.result.uploadedFileStatusId === 5 ? '' : response?.result);
          this.setState(
            {
              fileData: fileDatas === '' || uploadedFileStatusId == 5 ? '' : fileDatas,
              uploadedFileStatusId: uploadedFileStatusId,
              isRecord: false,
            },
            () => {
              callback();
            },
          );
        });
      } else {
        this.setState({ fileData: [] });
      }
    });
  }
  uploadtemplatestatus = async () => {
    if (this.state.fileData.length != 0) {
      var data = 'yes';
      var data1 = this.state.fileData.filter((e: any) => {
        return e.id != null;
      });
      data1.forEach((item: any) => {
        this.apiClient.updateInputTemplateData(item?.id, data);
        this.setState({ fileData: [] });
        // this.refresh();
      });
    }
  };
  async downloadTemplate(templateId: any) {
    try {
      var templateName = (await this.apiClient.getDownloadTemplate(templateId.id)).result;
      const token = this.token;
      const url = `${baseUrl}/api/Template/DownloadTemplate?templateId=${
        templateId.id
      }&format=xlsx&${Object.entries({ token })
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      // downloadFileCommon(url);
      downloadFileCommontemplate(url, templateName?.name);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  downloadFile(id: any) {
    try {
      // this.apiClient.getDownloadTemplate(id);
      const token = this.token;
      const url = `${baseUrl}/api/UploadFile/DownloadUploadedFile?uploadedFileId=${id}&${Object.entries(
        { token },
      )
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      downloadFileCommon(url);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  async publishFile() {
    const data = this.state.publishData;
    try {
      const publish = new UploadedFile();
      publish.id = data?.id;
      publish.name = data?.name;
      publish.blobUrl = data?.blobUrl;
      publish.tableName = data?.tableName;
      publish.templateId = data?.templateId;
      const response = await this.apiClient.publishFile(publish);
      this.setState({ confirmModel: false, isTemplateFile: false });
      if (response.hasError) {
        this.notify.showErrorNotify(response.message || this.props.t('ERROR_UNKNOWN'));
      } else {
        this.notify.showSuccessNotify(this.props.t('PUBLISHED_SUCCESSFULLY'));
      }
      this.listFiles(data.id, () => {
        this.setState({ isTemplateFile: true });
      });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  removeParentheses = (fileName: any) => {
    return fileName.replace(/\([^()]*\)/g, '');
  };

  async onUpload(files: any) {
    try {
      if (!files) {
        return new Promise(() => {});
      }
      var f: any = files[0];

      const fileName =
        this.removeParentheses(f?.name).split('.').slice(0, -1).join('.').trim() + '.xlsx';

      if (
        !this.state.isNew &&
        this.state.selectedRowUploadFile?.name?.trim()?.toLowerCase() !==
          fileName?.trim()?.toLowerCase()
      ) {
        this.setState({ visible: false }, () => {
          this.notify.showErrorNotify(`${this.props.t('UPLOAD_DOWNLOADED_FILE-ERROR_DIFF_FILE')}`);
        });
        return false;
      }

      const templateName =
        this.removeParentheses(this.state.uploadTemplateName?.text + '.xlsx')
          .split('.')
          .slice(0, -1)
          .join('.')
          .trim() + '.xlsx';

      const baseTemplateName = templateName.replace('.xlsx', '').trim();
      const validTemplatePattern = new RegExp(`^${baseTemplateName}(?:[- \\w]*)\\.xlsx$`, 'i');
      if (this.state.isNew && !validTemplatePattern.test(fileName)) {
        this.setState({ visible: false }, () => {
          this.notify.showErrorNotify(
            `${this.props.t('UPLOAD_DOWNLOADED_TEMPLATE-ERROR_DIFF_TEMPLATE')}`,
          );
        });
        return false;
      }

      this.setState({ visible: false, ProgressVisible: true, percentageComplete: f.name });
      const uri = this.state.isNew
        ? (await this.apiClient.getAuthorizedUrlForWrite(f?.name || ''))?.result
        : (
            await this.apiClient.getAuthorizedUrlForWriteForUrl(
              this.state?.selectedRowUploadFile?.blobUrl,
            )
          )?.result;
      var uploadData = new UploadedFile();
      uploadData.blobUrl = uri;
      uploadData.name = fileName;
      uploadData.id = this.state.isNew ? undefined : this.state?.selectedRowUploadFile?.id;
      uploadData.isActive = true;
      uploadData.tableName = 'Product';
      uploadData.templateId = this.state.isNew
        ? this.state.selectedTemplateId
        : this.state.selectedRowUploadFile?.templateId;
      const blobClient = new BlockBlobClient(uri || '');
      blobClient
        .uploadData(f, {
          onProgress: (observer) => {
            var progress = Math.round(observer.loadedBytes / f.size);
            this.setState({ progressPercent: progress });
          },
          blobHTTPHeaders: {
            blobContentType: f.type,
          },
        })
        .then(async (blobClientResponse) => {
          this.setState({ ProgressVisible: false });
          this.apiClient.uploadAndValidateFile(uploadData).then((r) => {
            if (r.hasError) {
              this.notify.showErrorNotify(
                r.message || this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'),
              );
            } else {
              this.listFiles(this.state.selectedTemplateId, () => {
                if (
                  this.state.uploadedFileStatusId === 5 ||
                  this.state.uploadedFileStatusId === 3
                ) {
                  this.notify.showSuccessNotify(this.props.t('VALIDATED_SUCCESSFULLY'));
                } else {
                  this.notify.showErrorNotify(this.props.t('VALIDATION_FAILED'));
                }
                this.setState({ isTemplateFile: true, getClick: true });
              });
              this.setState({ isTemplateFile: true });
            }
          });
          const clicked = 'true';
          sessionStorage.setItem('Tableclicked', clicked);
        });
    } catch (error) {
      this.setState({ ProgressVisible: false });
      this.notify.showErrorNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'));
    }
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  UploadPicture = async (image: any, file: any) => {
    try {
      this.setState({ imageProgress: false });
      this.onUpload(file);
    } catch (error: any) {
      this.setState({ imageProgress: false });
    }
  };

  UploadPicture1 = async (image: any) => {
    try {
      const uniqueFileNames = image.map((file: any) => {
        const fileName = file.name.toLowerCase();
        return fileName.substr(0, fileName.lastIndexOf('.'));
      });
      const uniqueFileNamesSet = new Set(uniqueFileNames);
      if (uniqueFileNamesSet.size !== uniqueFileNames.length) {
        this.notify.showErrorNotify(this.props.t('DUPLICATE_IMAGE'));
        return;
      }
      const uploadPromises = image.map(async (image: any) => {
        const imageData = new UploadPicture();
        imageData.name = image?.name;
        imageData.isActive = true;
        return this.apiClient.addPicture(imageData);
      });
      const responses = await Promise.all(uploadPromises);
      const valiedImage = responses.every((response) => !response.hasError);
      if (valiedImage) {
        for (var i = 0; i < image.length; i++) {
          const url = await this.apiClient.getAuthorizedUrlForWrite(image[i]?.name || '');
          if (!url.hasError) {
            this.setState({
              imageProgress: true,
              Imagetext: `uploading ${i + 1} of ${image.length}, ${image[i].name}`,
            });

            var imageData = new UploadPicture();
            imageData.imageUrl = url?.result;
            imageData.name = image[i]?.name;
            imageData.isActive = true;
            const response = await this.apiClient.addPicture(imageData);
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
        this.notify.showSuccessNotify(this.props.t('Image Added successfully'));
      }
      this.setState({ imageProgress: false });
    } catch (error: any) {
      this.setState({ imageProgress: false });
      throw error;
    }
  };

  setImages = (Imagefile: any) => {
    this.setState({
      uploadedImages: Imagefile,
    });
  };

  render() {
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '100px',
      },
      {
        key: 'TemplateName',
        name: <div className={columnHeader}>{t('TEMPLATE')}</div>,
        selector: (row: any) => <span title={row.text}>{row.text}</span>,
        isVisible: true,
        width: '300px',
      },
      {
        key: 'Action',
        name: <div className={columnHeader}>{t('ACTION')}</div>,
        selector: (row: any) => {
          return (
            <>
              {!row?.text?.toLowerCase().includes('buyer template 1') &&
                this.state.tempalteOptions.length !== 0 && (
                  <Grid lg={12} item container spacing={2} direction={'row'}>
                    <Grid item lg={6} xs={12}>
                      <DefaultButton
                        iconProps={{ iconName: 'Download' }}
                        className="button resetbutton"
                        styles={PIMButtons}
                        text={`${t('DOWNLOAD_TEMPLATE')}`}
                        onClick={() => this.downloadTemplate(row)}
                      />
                    </Grid>
                    <Grid item lg={6} xs={12}>
                      <DefaultButton
                        className="button resetbutton"
                        iconProps={{ iconName: 'Upload' }}
                        styles={PIMButtons}
                        text={t('BTN_UPLOAD')}
                        onClick={() => {
                          this.setState({
                            selectedTemplateId: row.id,
                            visible: true,
                            isNew: true,
                            uploadTemplateName: row,
                          });
                        }}
                      />
                    </Grid>
                  </Grid>
                )}

              {row?.text?.toLowerCase().includes('buyer template 1') &&
                this.state.tempalteOptions.length !== 0 && (
                  <Grid lg={20} item container spacing={2} direction={'row'}>
                    <Grid item lg={4.5} xs={12}>
                      <DefaultButton
                        iconProps={{ iconName: 'Download' }}
                        className="button resetbutton"
                        styles={PIMButtons}
                        text={`${t('DOWNLOAD_TEMPLATE')}`}
                        onClick={() => this.downloadTemplate(row)}
                      />
                    </Grid>
                    <Grid item lg={4.5} xs={12}>
                      <DefaultButton
                        className="button resetbutton"
                        iconProps={{ iconName: 'Upload' }}
                        styles={PIMButtons}
                        text={t('BTN_UPLOAD')}
                        onClick={() => {
                          this.setState({
                            selectedTemplateId: row.id,
                            visible: true,
                            isNew: true,
                            uploadTemplateName: row,
                          });
                        }}
                      />
                    </Grid>
                    {row?.text?.toLowerCase().includes('buyer template 1') && (
                      <Grid item lg={1.9} xs={12}>
                        <DefaultButton
                          iconProps={{ iconName: 'Upload' }}
                          styles={PIMButtons1}
                          text={t('IMAGE_UPLOAD')}
                          onClick={() => {
                            this.setState({ setVisible1: true });
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
        width: '500px',
      },
    ];
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.Columns) {
        this.setState({ Columns: column });
      } else {
        this.setState({ Columns: newColumns });
      }
    };
    const dataColumn: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('TEMPLATE_FILES')}`}</div>,
        selector: (row: any) => {
          const value = [
            row.id,
            this.state.tempalteOptions?.find((e: any) => e.id === row.templateId)?.text,
          ];
          return (
            <div>
              <Link to={{ pathname: `/input/template/filedata/${value}` }}>
                <span title={row.name}>{row.name}</span>
              </Link>
            </div>
          );
        },
        isVisible: true,
        width: '280px',
      },
      {
        key: 'template',
        name: <div className={columnHeader}>{`${t('TEMPLATE')}`}</div>,
        selector: (row: any) =>
          this.state.tempalteOptions?.find((e: any) => e.id === row.templateId)?.text,
        isVisible: true,
        width: '240px',
      },
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('STATUS')}`}</div>,
        selector: (row: any) => {
          return (
            <div>
              <div>
                {row.uploadedFileStatusId ? (
                  <span
                    className={
                      row.uploadedFileStatusId
                        ? `dataColumn${row.uploadedFileStatusId}`
                        : 'dataColumn'
                    }
                  >
                    <p>
                      {
                        this.state?.getUploadedFileStatus?.find(
                          (a: UploadedFileStatus) => a?.id === row?.uploadedFileStatusId,
                        )?.name
                      }
                    </p>
                  </span>
                ) : (
                  this.state.fileData.length != 0 && (
                    <span className={'dataColumn'}>
                      <p>{`${t('NIL')}`}</p>
                    </span>
                  )
                )}
              </div>
            </div>
          );
        },
        width: '100px',
      },
      {
        name: <div className={columnHeader}>{`${t('ACTION')}`}</div>,
        selector: (row: any) => {
          return (
            <>
              {this.state.fileData.length != 0 && (
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  <Grid item lg={7.6} xs={12}>
                    <DefaultButton
                      className="button"
                      iconProps={{ iconName: 'Download' }}
                      styles={PIMButtons}
                      text={`${t('DOWNLOAD_EXCEL')}`}
                      onClick={() => this.downloadFile(row.id)}
                      disabled={
                        row.uploadedFileStatusId == 5 || row.uploadedFileStatusId == 3
                          ? true
                          : false
                      }
                    />
                  </Grid>
                  {/* <Grid item lg={0.1} xs={12}></Grid>
                  <Grid item lg={3.8} xs={12}>
                    <DefaultButton
                      className="button"
                      styles={PIMButtons}
                      iconProps={{ iconName: 'Upload' }}
                      text={`${t('COL_BTN_UPLOAD')}`}
                      onClick={() =>
                        this.setState({ visible: true, isNew: false, selectedRowUploadFile: row })
                      }
                      disabled={
                        row.uploadedFileStatusId == 5 || row.uploadedFileStatusId == 3
                          ? true
                          : false
                      }
                    />
                  </Grid>
                  <Grid item lg={0.1} xs={12}></Grid> */}
                  <Grid item lg={3} xs={12}>
                    <DefaultButton
                      className="button"
                      styles={PIMButtons}
                      text={t('BTN_PUBLISH')}
                      onClick={() => {
                        this.setState({ confirmModel: true, publishData: row });
                      }}
                      disabled={row.uploadedFileStatusId == 3 ? false : true}
                    />
                  </Grid>
                </Grid>
              )}
            </>
          );
        },
        isVisible: true,
        width: '280px',
      },
      // {
      //   key: 'template',
      //   name:
      //     this.state.fileData &&
      //     this.state.fileData[0] &&
      //     this.state.fileData[0].uploadedFileStatusId === 4 ? (
      //       <div className={columnHeader}>
      //         <Icon
      //           iconName="Clear"
      //           title={t('BTN_CLEAR')}
      //           className="iconStyle cancelbtn"
      //           onClick={() => {
      //             this.uploadtemplatestatus();
      //           }}
      //         />
      //       </div>
      //     ) : null,
      //   isVisible:
      //     this.state.fileData &&
      //     this.state.fileData[0] &&
      //     this.state.fileData[0].uploadedFileStatusId === 4,
      //   width: '6%',
      // },
    ];
    const handleDataColumnReorder = (newColumns: any) => {
      var column: any = [...dataColumn];
      if (newColumns == this.state.dataColumns) {
        this.setState({ dataColumns: column });
      } else {
        this.setState({ dataColumns: newColumns });
      }
    };

    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Stack>
            <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
              'MENU_DATA_SYNC_TEMPLATE',
            )}`}</Text>
          </Stack>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={11.39} xs={0} />
            <Grid item lg={0.5} xs={12} style={{ marginTop: '-28px' }}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyleData"
                onClick={this.refresh.bind(this)}
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.confirmModel}
            containerClassName={PIMcontentStyles.confirmContainer}
            onDismiss={() =>
              this.setState({
                confirmModel: false,
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
                    confirmModel: false,
                  });
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody}>
              <Text variant="xLarge" className="apptext1">{`${t('CONFIRM_PUBLISH')}`}</Text>
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.publishFile.bind(this)}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonCancel}
                    text={`${t('BTN_CANCEL')}`}
                    onClick={() => {
                      this.setState({ confirmModel: false });
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>
          <Modal
            isOpen={this.state.visible}
            containerClassName={PIMcontentStyles.ExcelContainer}
            onDismiss={() =>
              this.setState({
                visible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <div className="apptext1">
                <h2>{t('UPLOAD')}</h2>
              </div>
              <IconButton
                styles={UploadStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ visible: false });
                }}
              />
            </div>
            <div className={PIMcontentStyles.body}>
              <UploadData
                ClosePopup={() => this.setState({ visible: false })}
                UploadData={(file: any, image: any) => this.UploadPicture(image, file)}
                Row={this.state?.uploadTemplateName}
              />
            </div>
          </Modal>
          <Modal
            isOpen={this.state.ProgressVisible}
            containerClassName={PIMcontentStyles.progressBarModal}
            onDismiss={() =>
              this.setState({
                ProgressVisible: false,
              })
            }
          >
            <div>
              <Text variant="small" className="">
                <b>{this.state.percentageComplete}</b>
              </Text>
            </div>
            <ProgressIndicator barHeight={20} percentComplete={this.state.progressPercent} />
          </Modal>
          <Modal
            isOpen={this.state.imageProgress}
            containerClassName={PIMcontentStyles.progressBarModal}
            onDismiss={() =>
              this.setState({
                imageProgress: false,
              })
            }
          >
            <div>
              <Text variant="small" className="">
                <b>{this.state.Imagetext}</b>
              </Text>
            </div>
            <ProgressIndicator barHeight={20} percentComplete={this.state.ImagePercent} />
          </Modal>

          <Modal
            isOpen={this.state.setVisible1}
            containerClassName={PIMcontentStyles.ExcelContainer}
            onDismiss={() => this.setState({ setVisible1: false })}
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
                  this.setState({ setVisible1: false });
                }}
              />
            </div>
            <div className={PIMcontentStyles.body}>
              <PsUploadImageFolder
                onUploadImage={(file: any) => this.UploadPicture1(file)}
                ClosePopup={() => this.setState({ setVisible1: false })}
              />
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
            <DataTable
              className="Datatemplate"
              columns={columns}
              data={this.state.tempalteOptions}
              pagination={true}
              onColumnOrderChange={handleColumnReorder}
              highlightOnHover
              responsive
              fixedHeader
              striped
              pointerOnHover
              fixedHeaderScrollHeight={
                this.state.fileData.length !== 0
                  ? this.state.isTemplateFile
                    ? '57vh'
                    : '70vh'
                  : '75.3vh'
              }
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
          {this.state.fileData.length !== 0 && (
            <IconButton
              className="button"
              iconProps={{ iconName: this.state.isTemplateFile ? 'ChevronDown' : 'ChevronRight' }}
              style={{ backgroundColor: 'white', color: 'black', marginTop:'-1px' }}
              ariaLabel="Template status"
              // onClick={() => this.setState({ isTemplateFile: !this.state.isTemplateFile })}
              onClick={this.handleIconButtonClick}
              // disabled={this.state.fileData[0].uploadedFileStatusId == 4 ? true : false}
            />
          )}
          {this.state.isTemplateFile && this.state.fileData.length !== 0 && (
            <div className="dataTableFixedFooter">
              <DataTable
                columns={dataColumn}
                data={this.state.fileData}
                onColumnOrderChange={handleDataColumnReorder}
                highlightOnHover
                responsive
                fixedHeader
                striped
                //fixedHeaderScrollHeight="59vh"
                paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
                paginationPerPage={this.Defaultperpage()}
                paginationRowsPerPageOptions={this.perpage()}
                noDataComponent={
                  <div className="noDataWidth">
                    {<DataTable columns={dataColumn} data={[{ '': '' }]} />}
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
        </div>
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(Data);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
