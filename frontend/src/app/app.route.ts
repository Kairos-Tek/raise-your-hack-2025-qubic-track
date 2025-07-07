import { Routes } from '@angular/router';

// dashboard
import { IndexComponent } from './index';
import { AppLayout } from './layouts/app-layout';
import { AuthLayout } from './layouts/auth-layout';
import { ResultsDashboardComponent } from './features/contract-audit/pages/results-dashboard/results-dashboard.component';
import { UploadAnalysisComponent } from './features/contract-audit/pages/upload-analysis/upload-analysis.component';
import { TestManagementComponent } from './features/contract-audit/pages/test-management/test-management.component';

export const routes: Routes = [
    {
        path: '',
        component: AppLayout,
        children: [
            // dashboard
            { path: '', component: UploadAnalysisComponent, data: { title: 'Upload Analysis' } },
            { path: 'audit/results', component: ResultsDashboardComponent, data: { title: 'Results Dashboard' } },
            { path: 'audit/upload', component: UploadAnalysisComponent, data: { title: 'Upload Analysis' } },
            { path: 'audit/test/:contractId', component: TestManagementComponent, data: { title: 'Test Management' } },
        ],
    },

    {
        path: '',
        component: AuthLayout,
        children: [],
    },
];
