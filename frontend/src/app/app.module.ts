import { NgModule } from '@angular/core';
import { BrowserModule, Title } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpBackend, HttpClient, HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

//Routes
import { routes } from './app.route';

import { AppComponent } from './app.component';

// store
import { StoreModule } from '@ngrx/store';
import { indexReducer } from './store/index.reducer';

// shared module
import { SharedModule } from 'src/shared.module';

// i18n
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
// AOT compilation support
export function HttpLoaderFactory(httpHandler: HttpBackend): TranslateHttpLoader {
    return new TranslateHttpLoader(new HttpClient(httpHandler));
}

// dashboard
import { IndexComponent } from './index';

// Layouts
import { AppLayout } from './layouts/app-layout';
import { AuthLayout } from './layouts/auth-layout';

import { HeaderComponent } from './layouts/header';
import { FooterComponent } from './layouts/footer';
import { SidebarComponent } from './layouts/sidebar';
import { ThemeCustomizerComponent } from './layouts/theme-customizer';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { ResultsDashboardComponent } from './features/contract-audit/pages/results-dashboard/results-dashboard.component';
import { UploadAnalysisComponent } from './features/contract-audit/pages/upload-analysis/upload-analysis.component';
import { TestManagementComponent } from './features/contract-audit/pages/test-management/test-management.component';
import { IconMenuAnalyticsComponent } from './shared/icon/menu/icon-menu-analytics';
import { IconDiamondComponent } from './shared/icon/icon-diamond';
import { IconUploadComponent } from './shared/icon/icon-upload';
import { IconBrainComponent } from './shared/icon/icon-brain';
import { PrimaryButtonComponent } from './shared/components/primary-button/primary-button.component';
import { BlockquoteComponent } from './shared/components/blockquote/blockquote.component';
import { IconTestComponent } from './shared/icon/icon-test';
import { CardComponent } from './shared/components/card/card.component';
import { AccordionComponent } from './shared/components/accordion/accordion.component';
import { DataTableModule } from '@bhplugin/ng-datatable';
import { IconCubeComponent } from './shared/icon/icon-cube';
import { CustomModalComponent } from './shared/components/modal/modal.component';
import { IconMonitorComponent } from './shared/icon/icon-monitor';
import { IconExperimentalComponent } from './shared/icon/icon-experimental';
import { ProgressBarComponent } from './shared/components/progress-bar/progress-bar.component';
import { IconRocketComponent } from './shared/icon/icon-rocket';

@NgModule({
    imports: [
        RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' }),
        BrowserModule,
        BrowserAnimationsModule,
        CommonModule,
        FormsModule,
        DataTableModule,
        HttpClientModule,
        TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpBackend],
            },
        }),
        StoreModule.forRoot({ index: indexReducer }),
        SharedModule.forRoot(),
    ],
    declarations: [
        AppComponent,
        HeaderComponent,
        FooterComponent,
        SidebarComponent,
        ThemeCustomizerComponent,
        IndexComponent,
        AppLayout,
        AuthLayout,
        LoaderComponent,
        ResultsDashboardComponent,
        UploadAnalysisComponent,
        TestManagementComponent,
        IconMenuAnalyticsComponent,
        IconDiamondComponent,
        IconUploadComponent,
        IconBrainComponent,
        PrimaryButtonComponent,
        BlockquoteComponent,
        IconTestComponent,
        CardComponent,
        AccordionComponent,
        IconCubeComponent,
        CustomModalComponent,
        IconMonitorComponent,
        IconExperimentalComponent,
        ProgressBarComponent,
        IconRocketComponent
    ],
    providers: [Title],
    bootstrap: [AppComponent],
})
export class AppModule {}
