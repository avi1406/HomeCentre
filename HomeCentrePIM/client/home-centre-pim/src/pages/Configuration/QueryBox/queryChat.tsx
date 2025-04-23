import { DefaultButton, Dropdown, IconButton, Image, Modal, ProgressIndicator, TextField } from '@fluentui/react';
import { Card, Grid } from '@mui/material';
import { t } from 'i18next';
import React, { Component } from 'react'
import ReactFormValidation from "react-form-input-validation";
import { Text } from '@fluentui/react/lib/Text';
import { chatBoxStyles, PIMButtons, PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonConfirm } from '../../PimStyles';
import ApiManager from '@/services/ApiManager';
import jwt_decode from "jwt-decode";
import { ProductQuery, ProductQueryComment } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import { cancelIcon, iconButtonStyles } from '@/common/properties';
import UploadImage from './UploadImage';
import { BlockBlobClient } from '@azure/storage-blob';
import axios from 'axios';
import { renderOption } from '@/pages/commonFunctions';

export default class QcChat extends Component<any, any> {
  form: ReactFormValidation
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  token = sessionStorage.getItem("token");
  decode: any = jwt_decode(this.token || '');
  userId: any = this.decode?.UserId;

  constructor(props: any) {
    super(props);
    this.state = {
      fields: {
        id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
        name: this.props.fields?.length == 1 ? this.props.fields[0].name : "",
        itemCode: this.props.fields?.length == 1 ? this.props.fields[0].itemCode : ""
      },
      templateSelected: this.props.fields?.length == 1 ? this.props.fields[0].templateId : null,
      errors: {},
      qcChater: [],
      assignTo: [],
      queryStatus: [],
      queryType: [],
      chatText: '',
      imgVisible: false,
      uploadImage: false,
      ProgressVisible: false,
      imageName: '',
      progressPercent: 0,
      isImageLoading: true,
      imageLoadProgress: 0,
      pictureURL: '',
      imageLoading: 'Loading image...',
    }
    this.form = new ReactFormValidation(this);
    this.form.useRules({
      name: "required",
      itemCode: "required"
    });

    this.form.onformsubmit = async (fields) => {
      // console.log(fields);
    }
  }

  async componentDidMount() {
    this.setState({ loading: true });
    this.refresh();
    this.setState({ loading: false });
  }
  async refresh() {
    var assign: any = [];
    var qrySts: any = [];
    var qryTyp: any = [];
    const response = (await this.apiClient.searchQueryComments('', 0, 0, this.props.rowData?.id)).result?.records
    const userOpt = (await this.apiClient.searchUsers(true)).result?.records
    const querySts = (await this.apiClient.getQcStatus()).result
    const queryType = (await this.apiClient.getQueryTypes()).result
    userOpt?.forEach((values: any) => {
      assign.push({ key: values.id, text: values.name, })
    })
    querySts?.forEach((data: any) => {
      qrySts.push({ key: data.id, text: data.name })
    })
    queryType?.forEach((e: any) => {
      qryTyp.push({ key: e.id, text: e.name })
    })
    this.setState({ qcChater: response, assignTo: assign, queryStatus: qrySts, queryType: qryTyp, chatText: '' })
  }
  async onImageLoad() {
    try {
      const value = this.props;
      const authToken = sessionStorage.getItem("token");
      const imageSrc = value.rowData.picture;
      const headers = new Headers();
      headers.append('Authorization', `Bearer ${authToken}`);

      axios(imageSrc, { headers })
        .then((response) => {
          this.setState({ isImageLoading: false });
        })
        .catch((error) => {
          this.setState({ imageLoading: 'Failed to load image' });
        });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }
  async onSend() {
    const create = new ProductQueryComment()
    create.comment = this.state.chatText
    create.productQueryId = this.props.rowData.id
    create.isActive = true
    const response = await this.apiClient.createQueryComment(create)
    if (response.hasError) {
      this.notify.showErrorNotify(`${t('MESSAGE_NOT_SEND')}`);
    } else {
      this.notify.showSuccessNotify(`${t('COMMENT_UPDATED')}`);
    }
    this.refresh();
  }
  changeUser = async (id: any) => {
    const queryIds: any = [this.props.rowData?.id];
    const response = await this.apiClient.reassignQuery(queryIds, id)
    if (response.hasError) {
      this.notify.showErrorNotify(t('UNKNOWN_ERROR'));
    } else {
      this.notify.showSuccessNotify(t('USER_UPDATED'));
    }
  }

  UploadImage = async (file: any) => {
    const url = (await this.apiClient.getAuthorizedUrlForWrite(file[0]?.name)).result
    this.setState({ uploadImage: false, ProgressVisible: true, imageName: file[0]?.name })
    const blobClient = new BlockBlobClient(url || '');
    blobClient.uploadData(file[0], {
      onProgress: (observer: any) => {
        const progress = Math.round((observer.loadedBytes / file[0].size) * 100)
        this.setState({ progressPercent: (progress / 100) })
      },
      blobHTTPHeaders: {
        blobContentType: file[0].type
      }
    }).then((r) => {
      this.setState({ ProgressVisible: false, uploadImage: false, imgVisible: false })
      const query = new ProductQuery()
      query.id = this.props.rowData?.id
      query.assignedToId = this.props.rowData?.assignedToId
      query.description = this.props.rowData?.description
      query.picture = url
      query.isActive = true
      query.productQueryStatusId = this.props.rowData?.productQueryStatusId
      query.queryPriorityId = this.props.rowData?.queryPriorityId
      query.queryTypeId = this.props.rowData?.queryTypeId
      query.productIds = [216]
      this.apiClient.createQuery(query).then((r: any) => {
        if (r.hasError == false) {
          this.notify.showSuccessNotify(t("IMAGE_UPDATED"));
          this.props.onLoad();
        } else {
          this.notify.showErrorNotify(t('UNKNOWN_ERROR'));
        }
      })
    }).catch((error) => {
      this.setState({ ProgressVisible: false })
    })
  }

  render() {
    return (
      <>
        <div style={chatBoxStyles}>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={6} xs={6}>
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={6} xs={12}>
                  <Text variant="large" className="text">
                    <b>{`${t('QUERY_TYPE')}`}</b>
                  </Text>
                </Grid>
                <Grid item lg={6} xs={12}>
                  <Text variant="large" className="text">
                    {': '}
                    {
                      this.state.queryType.find(
                        (e: any) => e.key == this.props.rowData?.queryTypeId,
                      )?.text
                    }
                  </Text>
                </Grid>
              </Grid>
              <br />
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={6} xs={12}>
                  <Text variant="large" className="text">
                    <b>{`${t('TO')}`}</b>
                  </Text>
                </Grid>
                <Grid item lg={6} xs={12}>
                  {this.props.isAssign == true ? (
                    <Text variant="large" className="text">
                      {': '}
                      {
                        this.state.assignTo.find(
                          (e: any) => e.key == this.props.rowData?.assignedToId,
                        )?.text
                      }
                    </Text>
                  ) : (
                    <Dropdown
                      className="dropdown"
                      options={this.state.assignTo}
                      styles={PIMdropdownStyles}
                      defaultSelectedKey={this.props.rowData?.assignedToId}
                      onChange={(e: any, selectedOption: any) =>
                        this.changeUser(selectedOption.key)
                      }
                      onRenderOption={renderOption}
                    />
                  )}
                </Grid>
              </Grid>
              <br />
              <Grid lg={12} item container spacing={2} direction={'row'}>
                <Grid item lg={6} xs={12}>
                  <Text variant="large" className="text">
                    <b>{`${t('QUERY_TYPE')}`}</b>
                  </Text>
                </Grid>
                <Grid item lg={6} xs={12}>
                  <Text
                    variant="large"
                    className={
                      this.props.rowData?.productQueryStatusId == 10
                        ? 'pendingQuery text'
                        : 'resolvedQuery text'
                    }
                  >
                    <b>
                      {': '}
                      {
                        this.props.QcStatus.find(
                          (e: any) => e.key == this.props.rowData?.productQueryStatusId,
                        )?.text
                      }
                    </b>
                  </Text>
                  {/* <TextField
                                label={`${t('QUERY_STATUS')}`}
                                value={this.props.QcStatus.find((e: any) => e.key == this.props.rowData?.productQueryStatusId)?.text}
                            /> */}
                </Grid>
              </Grid>
              <br />
            </Grid>
            <Grid item lg={1.5} xs={0} />
            <Grid item lg={3} xs={4}>
              <DefaultButton
                className="button"
                text={`${t('VIEW_IMAGE')}`}
                styles={PIMButtons}
                onClick={() => {
                  this.onImageLoad();
                  this.setState({ imgVisible: true });
                }}
              />
            </Grid>
            <Grid item lg={1.5} xs={0} />
          </Grid>
          <br />
          <Text variant="xLarge" className="text">
            {t('QUERY')}
          </Text>
          <div style={chatBoxStyles}>
            <Text className="text">
              <b>{`${this.props.rowData.description}`}</b>
            </Text>
          </div>
          <div>
            {this.state.qcChater.length == 0 && (
              <h2 className="noComments">{t('NO_COMMENTS')}</h2>
            )}
            <div className="chatHeightQc">
              {this.state.qcChater?.map((data: any) => {
                return (
                  <Card className={this.userId == data.createdBy ? 'chatSelf' : 'chatUser'}>
                    {this.userId != data.createdBy && (
                      <div className="chatBoxName">
                        {this.state.assignTo.find((e: any) => e.key == data.createdBy)?.text}
                      </div>
                    )}
                    <div>{data.comment}</div>
                  </Card>
                );
              })}
            </div>
            <Grid lg={12} item container spacing={1} direction={'row'}>
              <Grid item lg={10.6} xs={10}>
                <TextField
                  placeholder={`${t('INPUT_TEXT_HERE')}`}
                  className="textField"
                  onChange={(e: any) => this.setState({ chatText: e.target.value })}
                  value={this.state.chatText}
                />
              </Grid>
              <Grid item lg={1} xs={2}>
                <DefaultButton
                  className="button"
                  iconProps={{ iconName: 'Send' }}
                  disabled={this.state.chatText.trim() == '' ? true : false}
                  styles={PIMfooterButtonConfirm}
                  onClick={this.onSend.bind(this)}
                />
              </Grid>
            </Grid>
          </div>
          <Modal
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
              <span>{t('IMAGE')}</span>
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ imgVisible: false });
                }}
              />
            </div>
            <div className={PIMcontentStyles.imagebody}>
              {this.state.isImageLoading ? (
                <h3>{this.state.imageLoading}</h3>
              ) : this.props.rowData?.picture ? (
                <Image
                  src={this.props.rowData?.picture}
                  alt="picture"
                  style={{ maxWidth: '500px', maxHeight: '400px' }}
                />
              ) : (
                <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
              )}
            </div>
            {this.props.isAssign == false && (
              <div className={PIMcontentStyles.footer}>
                <Grid lg={12} item container spacing={2}>
                  <Grid item lg={2} xs={12} />
                  <Grid item lg={8} xs={12}>
                    <DefaultButton
                      className="button"
                      styles={PIMfooterButtonConfirm}
                      iconProps={{ iconName: 'CloudUpload' }}
                      text={`${t('UPLOAD_IMAGE')}`}
                      onClick={() => this.setState({ uploadImage: true })}
                    />
                  </Grid>
                </Grid>
              </div>
            )}
          </Modal>
          <Modal
            isOpen={this.state.uploadImage}
            containerClassName={PIMcontentStyles.ExcelContainer}
            onDismiss={() =>
              this.setState({
                uploadImage: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <div>
                <h2>{t('UPLOAD')}</h2>
              </div>
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ uploadImage: false });
                }}
              />
            </div>
            <div className={PIMcontentStyles.body}>
              <UploadImage
                ClosePopup={() => this.setState({ uploadImage: false })}
                UploadImage={(file: any) => this.UploadImage(file)}
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
            <div style={{ marginTop: '8px' }}>
              <Text variant="small" className="text">
                <b>{this.state.imageName}</b>
              </Text>
            </div>
            <ProgressIndicator percentComplete={this.state.progressPercent} barHeight={20} />
          </Modal>
        </div>
      </>
    );
  }
}
