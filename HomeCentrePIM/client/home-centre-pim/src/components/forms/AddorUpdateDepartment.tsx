import ApiManager from '@/services/ApiManager';
import React, { Component } from 'react'
import { cancelButton, submitButton } from "../../common/properties";
import { Checkbox, DefaultButton, Dropdown, TextField } from "@fluentui/react";
import ReactFormValidation from "react-form-input-validation";
import { Grid } from "@mui/material";
export default class AddorUpdateDepartment extends Component<any, any> {

    apiClient = new ApiManager().CreateApiClient();
    form: ReactFormValidation;
    isRole: any;
    state: any
    message: string | undefined;
    constructor(props: any) {
        super(props);
        this.state = {
            fields: {
                id: this.props.fields?.length == 1 ? this.props.fields[0].id : 0,
                name: this.props.fields?.length == 1 ? this.props.fields[0].name : "",
            },
            isPlanogram: this.props.fields?.length == 1 ? this.props.fields[0].isPlanogram : false,
            divisionId: this.props.fields?.length == 1 ? this.props.fields[0].divisionId : null,
            divisionOption: this.props.option,
            errors: {}
        };
        this.form = new ReactFormValidation(this);
        this.form.useRules({
            name: "required|between:2,30|regex:/^[a-zA-Z ]*$/",
            isPlanogram: "required",
            division: "required"
        });
    }

    render() {
        return (
            <div style={{ maxWidth: "400px", margin: "0 auto" }}>
                <Grid>
                    <form
                        className="myForm"
                        noValidate
                        autoComplete="off"
                        onSubmit={this.form.handleSubmit}
                    >
                        <div>
                            <Grid style={{ display: "block", marginTop: "0px", padding: 10 }}>
                                <Grid>
                                    <TextField
                                        required
                                        label={`${this.props.t('ROLE_TABLE_NAME')}`}
                                        name="name"
                                        onBlur={this.form.handleBlurEvent}
                                        onChange={(event: any) => this.form.handleChangeEvent(event)}
                                        value={this.state.fields.name}
                                        data-attribute-name="Name"
                                        data-async
                                        className='textField'
                                        errorMessage={this.state.errors.name ? this.state.errors.name == "The Name format is invalid." ? "The Name Allowes only Alphabets" : this.state.errors.name : ""}
                                    />
                                </Grid>
                            </Grid>
                            <Grid style={{ display: "block", marginTop: "0px", padding: 10 }} >
                                <Grid>
                                    <Dropdown
                                        className='dropdown'
                                        label={`${this.props.t('PLANOGRAM_DEPARTMENT')}`}
                                        selectedKey={this.state.divisionId}
                                        onChange={(e: any, item: any) => this.setState({ divisionId: item.key })}
                                        placeholder="Select an option"
                                        options={this.state.divisionOption}
                                        styles={{ dropdown: { width: "100%" } }}
                                    />
                                </Grid>
                            </Grid>
                            <Grid style={{ display: "block", marginTop: "0px", padding: 10 }}>
                                <Grid>
                                    <Checkbox
                                        defaultChecked={this.state.isPlanogram}
                                        label="Is Planogram" onChange={(ev?: React.FormEvent<HTMLElement | HTMLInputElement>, isChecked?: boolean) => this.setState({ isPlanogram: isChecked })} />
                                </Grid>
                            </Grid>
                            <Grid style={{ display: "block", marginTop: "0px", padding: 10 }}>
                                <Grid>
                                    <DefaultButton style={{ left: '5%' }} className='button' styles={cancelButton} onClick={this.props.ClosePopup}>{this.props.t('BTN_CANCEL')}</DefaultButton>
                                    <DefaultButton
                                        className='button'
                                        style={{ right: '-20%' }}
                                        styles={submitButton}
                                        onClick={this.form.onformsubmit}
                                        disabled={
                                            this.props.fields.length == 1 ?
                                                !(this.props.fields[0].name == this.state.fields.name) || !(this.state.divisionId == this.props.fields[0].divisionId) || !(this.state.isPlanogram == this.props.fields[0].isPlanogram) ? false : true
                                                :
                                                this.state.divisionId == null || this.state.fields.name == ""
                                        }
                                    >{this.props.t(this.state.fields.id != 0 ? 'EDIT_BTN_SUBMIT' : 'ADD_BTN_SUBMIT')}</DefaultButton>
                                </Grid>
                            </Grid>
                        </div>
                    </form>
                </Grid>
            </div>
        )
    }
}
