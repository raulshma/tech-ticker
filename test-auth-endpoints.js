#!/usr/bin/env node

/**
 * Test script to verify login and register endpoints work end-to-end
 * This script tests the flow from frontend API calls through the reverse proxy to the user service
 */

const https = require('https');
const http = require('http');

// Configuration
const REVERSE_PROXY_URL = 'https://localhost:7069';
const TEST_USER = {
  firstName: 'Test',
  lastName: 'User',
  email: `testuser${Date.now()}@example.com`,
  password: 'TestPassword123!'
};

// Disable SSL certificate validation for testing
process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = 0;

/**
 * Make HTTP request
 */
function makeRequest(options, data = null) {
  return new Promise((resolve, reject) => {
    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => {
        body += chunk;
      });
      res.on('end', () => {
        try {
          const jsonBody = JSON.parse(body);
          resolve({
            statusCode: res.statusCode,
            headers: res.headers,
            body: jsonBody
          });
        } catch (e) {
          resolve({
            statusCode: res.statusCode,
            headers: res.headers,
            body: body
          });
        }
      });
    });

    req.on('error', (err) => {
      reject(err);
    });

    if (data) {
      req.write(JSON.stringify(data));
    }
    req.end();
  });
}

/**
 * Test user registration
 */
async function testRegister() {
  console.log('🧪 Testing user registration...');
  
  const options = {
    hostname: 'localhost',
    port: 7069,
    path: '/api/v1/users/register',
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'User-Agent': 'TechTicker-Test/1.0'
    }
  };

  try {
    const response = await makeRequest(options, TEST_USER);
    console.log(`📊 Register Response Status: ${response.statusCode}`);
    console.log(`📊 Register Response Body:`, response.body);
    
    if (response.statusCode === 200 || response.statusCode === 201) {
      console.log('✅ Registration test PASSED');
      return response.body;
    } else {
      console.log('❌ Registration test FAILED');
      return null;
    }
  } catch (error) {
    console.error('❌ Registration test ERROR:', error.message);
    return null;
  }
}

/**
 * Test user login
 */
async function testLogin() {
  console.log('🧪 Testing user login...');
  
  const loginData = {
    email: TEST_USER.email,
    password: TEST_USER.password
  };

  const options = {
    hostname: 'localhost',
    port: 7069,
    path: '/api/v1/users/login',
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'User-Agent': 'TechTicker-Test/1.0'
    }
  };

  try {
    const response = await makeRequest(options, loginData);
    console.log(`📊 Login Response Status: ${response.statusCode}`);
    console.log(`📊 Login Response Body:`, response.body);
    
    if (response.statusCode === 200) {
      console.log('✅ Login test PASSED');
      return response.body;
    } else {
      console.log('❌ Login test FAILED');
      return null;
    }
  } catch (error) {
    console.error('❌ Login test ERROR:', error.message);
    return null;
  }
}

/**
 * Test reverse proxy health
 */
async function testReverseProxyHealth() {
  console.log('🧪 Testing reverse proxy health...');
  
  const options = {
    hostname: 'localhost',
    port: 7069,
    path: '/health',
    method: 'GET',
    headers: {
      'User-Agent': 'TechTicker-Test/1.0'
    }
  };

  try {
    const response = await makeRequest(options);
    console.log(`📊 Health Check Status: ${response.statusCode}`);
    
    if (response.statusCode === 200) {
      console.log('✅ Reverse proxy health check PASSED');
      return true;
    } else {
      console.log('❌ Reverse proxy health check FAILED');
      return false;
    }
  } catch (error) {
    console.error('❌ Reverse proxy health check ERROR:', error.message);
    return false;
  }
}

/**
 * Main test function
 */
async function runTests() {
  console.log('🚀 Starting TechTicker Auth End-to-End Tests');
  console.log('=' .repeat(50));
  
  // Test reverse proxy health first
  const healthOk = await testReverseProxyHealth();
  if (!healthOk) {
    console.log('❌ Cannot proceed with tests - reverse proxy is not healthy');
    process.exit(1);
  }
  
  console.log('');
  
  // Test registration
  const registerResult = await testRegister();
  console.log('');
  
  // Test login (only if registration succeeded)
  if (registerResult) {
    const loginResult = await testLogin();
    console.log('');
    
    if (loginResult) {
      console.log('🎉 All tests PASSED! Login and register are working end-to-end.');
    } else {
      console.log('⚠️  Registration works but login failed.');
    }
  } else {
    console.log('⚠️  Registration failed, skipping login test.');
  }
  
  console.log('=' .repeat(50));
  console.log('✨ Test run completed');
}

// Run the tests
runTests().catch(console.error);
