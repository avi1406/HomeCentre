import { cancelIcon, iconButtonStyles } from '@/common/properties'
import { ProductQuery } from '@/services/ApiClient'
import ApiManager from '@/services/ApiManager'
import NotificationManager from '@/services/NotificationManager'
import { BlockBlobClient } from '@azure/storage-blob'
import { Dropdown, IconButton, Modal, TextField } from '@fluentui/react'
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material'
import { t } from 'i18next'
import { DefaultButton, ProgressIndicator } from 'office-ui-fabric-react'
import React, { Component } from 'react'
import ReactFormValidation from "react-form-input-validation";
import { PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm } from '../../PimStyles'
import AddImage from './AddImage'

export default class QueryRequestForm extends Component<any, any> {
    form: ReactFormValidation
    apiClient = new ApiManager().CreateApiClient();
    notify = new NotificationManager();
    constructor(props: any) {
        super(props);
        var itemCodeArray = this.props.rowData?.map((element: any) => element.uid);
        const productIds = props.rowData?.map((element: any) => element.id[0]);
        this.state = {
            fields: {
                itemCode: this.props.rowData[0]?.uid == '' ? '' : itemCodeArray.join(', '),
                subject: '',
                queryDiscription: ''
            },
            errors: {},
            queryTypeOption: [],
            assignTo: [],
            queryType: null,
            assignToId: null,
            addPicture: false,
            picture: '',
            picUrl: '',
            progressPercent: 0,
            ProgressVisible: false,
        }

        this.form = new ReactFormValidation(this);
        this.form.useRules({
            itemCode: "regex:/^[a-zA-Z0-9,_ \s]*$/",
            name: "required",
            queryDiscription: "required",
            picture: 'string',
        });

        this.form.onformsubmit = async (fields) => {
            this.props.CloseModal();
            const query = new ProductQuery()
            query.productIds = productIds;
            query.assignedToId = this.state.assignToId
            query.queryTypeId = this.state.queryType
            query.queryPriorityId = 1
            query.description = fields.queryDiscription
            query.picture = this.state.picUrl
            query.productQueryStatusId = 10
            const response: any = await this.apiClient.createQuery(query)
            if (response.hasError) {
                this.notify.showErrorNotify(response.message)
            } else {
                this.notify.showSuccessNotify(t('QUERY_ADDED_SUCCESSFULLY'));
            }
        }
    }

    async componentDidMount() {
        var queryType: any = [];
        var assign: any = [];
        const queryOpt = (await this.apiClient.getQueryTypes()).result
        const userOpt = (await this.apiClient.searchUsers(true)).result?.records
        queryOpt?.forEach((value: any) => {
            queryType.push({
                key: value.id,
                text: value.name,
            })
        })
        userOpt?.forEach((values: any) => {
            assign.push({
                key: values.id,
                text: values.name,
            })
        })
        this.setState({ queryTypeOption: queryType, assignTo: assign });
    }
    imageUpload = async (file: any) => {
        this.setState({ picture: file[0]?.name, addPicture: false, ProgressVisible: true });
        const url = (await this.apiClient.getAuthorizedUrlForWrite(file[0]?.name)).result
        const blobClient = new BlockBlobClient(url || '');
        blobClient.uploadData(file[0], {
            onProgress: (observer: any) => {
                const progress = Math.round((observer.loadedBytes / file[0].size) * 100)
                this.setState({ progressPercent: (progress / 100) })
            },
            blobHTTPHeaders: {
                blobContentType: file[0].type
            }
        }).then((response: any) => {
            this.setState({ ProgressVisible: false, picUrl: url });
        }).catch((error: any) => {
            this.notify.showErrorNotify(error);
        })

    }
    render() {
        return (
            <>
                <form
                    className="myForm"
                    noValidate
                    autoComplete="off"
                    onSubmit={this.form.handleSubmit}
                >
                    <Grid lg={12} item container spacing={2} direction={"row"}>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={4} xs={12}>
                                <TextField
                                    name='itemCode'
                                    required
                                    className='textField'
                                    label={`${t('ITEM_CODE')}`}
                                    placeholder={`${t('ITEM_CODE')}`}
                                    value={this.state.fields.itemCode}
                                    errorMessage={this.state.errors.itemCode ? this.state.errors.itemCode : ""}
                                />
                            </Grid>
                            <Grid item lg={2} xs={0} />
                            <Grid item lg={4} xs={12}>
                                <Dropdown
                                    required
                                    className='dropdown'
                                    id='queryType'
                                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                                    label={`${t('QUERY_TYPE')}`}
                                    options={this.state.queryTypeOption}
                                    onChange={(e: any, selectedOption: any) => this.setState({ queryType: selectedOption.key })}
                                    errorMessage={this.state.errors.queryType ? this.state.errors.queryType : ""}
                                    styles={PIMdropdownStyles}
                                />
                            </Grid>
                        </Grid>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={4} xs={12}>
                                <Dropdown
                                    required
                                    className='dropdown'
                                    id='to'
                                    placeholder={`${t('TO')}`}
                                    label={`${t('TO')}`}
                                    options={this.state.assignTo}
                                    onChange={(e: any, selectedOption: any) => this.setState({ assignToId: selectedOption.key })}
                                    errorMessage={this.state.errors.to ? this.state.errors.to : ""}
                                    styles={PIMdropdownStyles}
                                />
                            </Grid><Grid item lg={2} xs={0} />
                            <Grid item lg={4} xs={12}>
                                <TextField
                                    name='picture'
                                    className='textField'
                                    label={`${t('PICTURE')}`}
                                    placeholder={`${t('PICTURE_PLACEHOLDER')}`}
                                    value={this.state.picture}
                                    iconProps={{ iconName: 'Photo2Add' }}
                                    onClick={() => this.setState({ addPicture: true })}
                                />
                            </Grid>
                        </Grid>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={7} xs={12} >
                                <TextField
                                    name='queryDiscription'
                                    required
                                    className='textField'
                                    label={`${t('QUERY_DISCRIPTION')}`}
                                    placeholder={`${t('QUERY')}`}
                                    value={this.state.fields.queryDiscription}
                                    onBlur={this.form.handleBlurEvent}
                                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                                    errorMessage={this.state.errors.queryDiscription ? this.state.errors.queryDiscription : ""}
                                    multiline
                                />
                            </Grid>
                        </Grid>
                    </Grid>
                    <Modal
                        isOpen={this.state.addPicture}
                        containerClassName={PIMcontentStyles.ExcelContainer}
                    >
                        <div className={PIMcontentStyles.header}>
                            <div className='apptext'>
                                <h2 >{t('UPLOAD')}</h2>
                            </div>
                            {/* <IconButton
                                styles={iconButtonStyles}
                                iconProps={cancelIcon}
                                ariaLabel="Close popup modal"
                                onClick={() => {
                                    this.setState({ addPicture: false })
                                }}
                            /> */}
                        </div>
                        <div className={PIMcontentStyles.body}>
                            <AddImage
                                ClosePopup={() => this.setState({ addPicture: false })}
                                UploadImage={(file: any) => this.imageUpload(file)}
                            />
                        </div>
                    </Modal>
                    <Modal isOpen={this.state.ProgressVisible} containerClassName={PIMcontentStyles.progressBarModal}  >
                        <div style={{ marginTop: '8px' }} >
                            <Text variant="small" className="text"><b>{this.state.picture}{" uploading..."}</b></Text>
                        </div>
                        <ProgressIndicator percentComplete={this.state.progressPercent} barHeight={20} />
                    </Modal >
                    <div className={PIMcontentStyles.footer}>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={1.5} xs={12} />
                            <Grid item lg={4} xs={12} >
                                <DefaultButton
                                    className='button'
                                    styles={PIMfooterButtonCancel}
                                    text={`${t('BTN_CANCEL')}`}
                                    onClick={() => this.props.CloseModal()}
                                />
                            </Grid>
                            <Grid item lg={0.5} xs={12} />
                            <Grid item lg={4} xs={12}>
                                <DefaultButton
                                    className='button'
                                    styles={PIMfooterButtonConfirm}
                                    type='submit'
                                    text={`${t('BTN_SUBMIT_REQUEST')}`}
                                    disabled={
                                        this.state.queryType == null || this.state.assignToId == null || this.state.fields.queryDiscription == "" ? true : false
                                    }
                                />
                            </Grid>
                            <Grid item lg={2} xs={12} />
                        </Grid>
                    </div>
                </form>
            </>
        )
    }
}