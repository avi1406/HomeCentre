import { DefaultButton, Dropdown, TextField } from '@fluentui/react';
import { Grid } from '@mui/material';
import { t } from 'i18next';
import React, { Component } from 'react';
import ReactFormValidation from "react-form-input-validation";
import ApiManager from '@/services/ApiManager';
import { MarketplaceAttribute } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import { PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm } from '../../PimStyles';

export default class EditMarketPlaceAttribute extends Component<any, any> {
    form: ReactFormValidation
    apiClient = new ApiManager().CreateApiClient();
    notify = new NotificationManager();


    constructor(props: any) {
        super(props);
        this.state = {
            fields: {
                id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
                Attribute: this.props.fields?.length == 1 ? this.props.fields[0].name : "",
                description: this.props.fields?.length == 1 ? this.props.fields[0].description : ""
            },
            DataType: this.props.fields?.length == 1 ? this.props.fields[0].dataType : null,
            errors: {},
        }
        this.form = new ReactFormValidation(this);
        this.form.useRules({
            Attribute: "required",
            description: "present"
        });

        this.form.onformsubmit = async (fields) => {
            const selectedRow = this.props.fields[0];
            var body = new MarketplaceAttribute();
            body.id = selectedRow.id;
            body.isActive = true;
            body.marketplaceId = selectedRow.marketplaceId;
            body.name = fields.Attribute;
            body.description = fields.description;
            body.dataType = this.state.DataType;
            body.divisionId = this.props.DivisionId;
            this.apiClient.addOrUpdateMarketplaceAttribute(body).then((r: any) => {
                let message = r.hasError == false ? `${this.props.t('MSG_UPDATE')}` : `${this.props.t('ERROR_UNKNOWN')}`
                if (r.hasError) {
                    this.notify.showErrorNotify(r.message);
                } else {
                    this.notify.showSuccessNotify(message);
                }
            })
            this.props.ClosePopup();
        }
    }
    render() {
        var { description, dataType, name } = this.props.fields[0];
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
                                    name='Attribute'
                                    className='textField'
                                    label={`${t('ATTRIBUTES')}`}
                                    required
                                    placeholder={`${t('DR_NAME_PLACEHOLDER')}`}
                                    value={this.state.fields.Attribute}
                                    onBlur={this.form.handleBlurEvent}
                                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                                    errorMessage={this.state.errors.Attribute ? this.state.errors.Attribute : ""}
                                />
                            </Grid>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={4.5} xs={12}>
                                <Dropdown
                                    className='dropdown'
                                    options={this.props.DataTypeOption}
                                    label={`${t('DATA_TYPE')}`}
                                    styles={PIMdropdownStyles}
                                    required
                                    placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                                    selectedKey={this.state.DataType}
                                    errorMessage={this.state.errors.templateSelected ? this.state.errors.templateSelected : ""}
                                    onChange={(e: any, item: any) => this.setState({ DataType: item.key })}
                                />
                            </Grid>
                        </Grid>
                        <Grid lg={12} item container spacing={0}>
                            <Grid item lg={1} xs={0} />
                            <Grid item lg={7} xs={12}>
                                <TextField
                                    name='description'
                                    className='textField'
                                    label={`${t('ATTRIBUTE_DISCRIPTION')}`}
                                    multiline
                                    autoAdjustHeight
                                    required
                                    placeholder={`${t('DR_ITEM_PLACEHOLDER')}`}
                                    value={this.state.fields.description}
                                    onBlur={this.form.handleBlurEvent}
                                    onChange={(event: any) => this.form.handleChangeEvent(event)}
                                    errorMessage={this.state.errors.name ? this.state.errors.name : ""}
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
                                    text={`${t('EDIT_BTN_SUBMIT')}`}
                                    styles={PIMfooterButtonConfirm}
                                    disabled={!(description == this.state.fields.description) || !(name == this.state.fields.Attribute) || !(dataType == this.state.DataType) ? false : true}
                                />
                            </Grid>
                            <Grid item lg={1} xs={12} />
                            <Grid item lg={5} xs={12}>
                                <DefaultButton
                                    className='button'
                                    text={`${t('DR_BTN_CANCEL')}`}
                                    styles={PIMfooterButtonCancel}
                                    onClick={() => this.props.CloseOnly()}
                                />
                            </Grid>
                        </Grid>
                    </div>
                </form>
            </div>
        )
    }
}
