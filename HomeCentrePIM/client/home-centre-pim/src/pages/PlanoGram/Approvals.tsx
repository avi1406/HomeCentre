import ApiManager from '@/services/ApiManager';
import React, { Component, Suspense } from 'react'
import { withTranslation } from 'react-i18next';
import jwt_decode from "jwt-decode";
import { Text } from '@fluentui/react/lib/Text';
import { t } from 'i18next';
import { Stack } from '@fluentui/react';
import { containerStackTokens, createPlanogram } from '@/common/properties';
import ApprovalL1 from './ApprovalL1';
import ApprovalL2 from './ApprovalL2';

export class Approvals extends Component<any, any> {
    isApprovalL1: boolean | undefined;
    apiClient = new ApiManager().CreateApiClient();
    token = sessionStorage.getItem("token");
    decode: any = jwt_decode(this.token || '');
    roleId: any = this.decode.RoleId.split(',');
    rolelvl: any = this.decode.RoleLevel;

    constructor(props: any) {
        super(props);
        this.state = {
            isApproval1: false,
            isApproval2: false
        }
    }

    async componentDidMount() {
        const configValue: any = await this.apiClient.getAppConfig();
        const appConfiguration = JSON.parse(configValue?.result?.find((e: any) => e.name == "ApproversList").jsonValue);
        const isApproval1 = this.roleId.filter(function (obj: any) {
            return appConfiguration.FirstLevelApprovers.indexOf(parseInt(obj)) !== -1;
        }).length > 0;
        const isApprovalLevel2 = this.roleId.filter((element: any) => appConfiguration.SecondLevelApprovers.includes(parseInt(element))).length > 0
        const isApproval2 = isApproval1 ? false : isApprovalLevel2;
        this.setState({
            isApproval1: isApproval1, isApproval2: isApproval2,
        });
    }

    render() {
        var { isApproval1, isApproval2 } = this.state;
        return (
            <div className='layout width-100'>
                {
                    (isApproval1 && this.rolelvl == "Level 1") ? <ApprovalL1 t={t} /> :
                        (isApproval2 && this.rolelvl == "Level 1") ? <ApprovalL2 t={t} /> :
                        (this.rolelvl == 'Level 1' && this.roleId.includes('5')) || (this.rolelvl == 'Level 1' && this.roleId.includes('2')) || this.state.familyid == undefined ? <Text variant='xxLarge' className="color-blue text">{`${this.props.t('LOADING_MESSAGE')}`}</Text> :
                                <Stack enableScopedSelectors tokens={containerStackTokens} styles={createPlanogram}>
                                    <Text variant='xxLarge' className="color-blue text">{`${this.props.t('NO_ACCESS_APPROVALS')}`}</Text>
                                </Stack>
                }
            </div>
        )
    }
}

const ComponentTranslated = withTranslation()(Approvals);
function App() {
    return (
        <Suspense fallback="">
            <ComponentTranslated />
        </Suspense>
    );
}
export default App;
