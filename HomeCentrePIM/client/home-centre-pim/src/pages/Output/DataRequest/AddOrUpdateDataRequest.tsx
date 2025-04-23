import { DefaultButton, Dropdown, TextField } from '@fluentui/react';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import React, { Component } from 'react'
import { PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm } from '../../PimStyles';
import ReactFormValidation from "react-form-input-validation";
import ApiManager from '@/services/ApiManager';
import { DataRequest } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';

export default class AddOrUpdateDataRequest extends Component<any, any> {
    form: ReactFormValidation
    apiClient = new ApiManager().CreateApiClient();
    notify = new NotificationManager();

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
        }
        this.form = new ReactFormValidation(this);
        this.form.useRules({
            name: "required",
            itemCode: "required"
        });

        this.form.onformsubmit = async (fields) => {
            const item = fields.itemCode.split(/[,\n]/);
            let itemCodes = item.filter(function (string: any) {
                return string.trim() !== "";
            });
            this.props.ClosePopup();
            var datarequest = new DataRequest()
            datarequest.name = fields.name
            datarequest.templateId = this.state.templateSelected
            datarequest.itemCodes = itemCodes
            const response: any = await this.apiClient.createDataRequest(datarequest)
            if (response.hasError) {
                this.notify.showErrorNotify(response?.message);
            } else if (!response.hasError) {
                this.notify.showSuccessNotify(t('MSG_ADDED_SUCCESSFULLY'));
                this.props.reload();
            }
        }
    }
    render() {
        return (
            <div>

                <form
                    className="myForm"
                    noValidate
                    autoComplete="off"
                    onSubmit={this.form.handleSubmit}
                >
                    <div className={PIMcontentStyles.attributeBody}>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={4.5} xs={12}>
                                <TextField
                                    name='name'
                                    className='textField'
                                    label={`${t('NEW_DATA_REQUESTNAME')}`}
                                    required
                                    placeholder={`${t('DR_NAME_PLACEHOLDER')}`}
                                    value={this.state.fields.name}
                                    onBlur={this.form.handleBlurEvent}
                                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                                    errorMessage={this.state.errors.name ? this.state.errors.name : ""}
                                />
                            </Grid>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={4.5} xs={12}>
                                <Dropdown
                                    className='dropdown'
                                    options={this.props.templateDropdown}
                                    label={`${t('NEW_DATA_REQUEST_TEMPLATES')}`}
                                    styles={PIMdropdownStyles}
                                    required
                                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                                    selectedKey={this.state.templateSelected}
                                    errorMessage={this.state.errors.templateSelected ? this.state.errors.templateSelected : ""}
                                    onChange={(e: any, item: any) => this.setState({ templateSelected: item.key })}
                                />
                            </Grid>
                        </Grid>
                        <Grid lg={12} item container spacing={0}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={7} xs={12}>
                                <TextField
                                    name='itemCode'
                                    className='textField'
                                    label={`${t('NEW_DATA_REQUEST_ENTER_ITEM_CODE')}`}
                                    multiline
                                    autoAdjustHeight
                                    required
                                    placeholder={`${t('DR_ITEM_PLACEHOLDER')}`}
                                    value={this.state.fields.itemCode}
                                    onBlur={this.form.handleBlurEvent}
                                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                                    errorMessage={this.state.errors.itemCode ? this.state.errors.itemCode : ""}
                                />
                            </Grid>
                        </Grid>
                    </div>
                    <div className={PIMcontentStyles.footer}>
                        <Grid lg={12} item container spacing={2} direction={"row"}>
                            <Grid item lg={6} xs={12}>
                                <DefaultButton
                                    type='submit'
                                    className='button'
                                    text={`${t('DR_BTN_UPLOAD')}`}
                                    styles={PIMfooterButtonConfirm}
                                />
                            </Grid>
                            <Grid item lg={1} xs={12} />
                            <Grid item lg={5} xs={12}>
                                <DefaultButton
                                    text={`${t('DR_BTN_CANCEL')}`}
                                    className='button'
                                    styles={PIMfooterButtonCancel}
                                    onClick={() => this.props.ClosePopup()}
                                />
                            </Grid>
                        </Grid>
                    </div>
                </form>
            </div>
        )
    }
}
