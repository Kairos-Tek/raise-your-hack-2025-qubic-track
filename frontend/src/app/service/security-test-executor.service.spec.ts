import { TestBed } from '@angular/core/testing';

import { SecurityTestExecutorService } from './security-test-executor.service';

describe('SecurityTestExecutorService', () => {
    let service: SecurityTestExecutorService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(SecurityTestExecutorService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
