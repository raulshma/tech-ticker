using Microsoft.Extensions.DependencyInjection;
using TechTicker.Shared.Utilities.Html;

namespace TechTicker.Shared.Tests;

public class TableParserTests
{
private readonly ITableParser _parser;

    public TableParserTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .AddScoped<ITableParser, UniversalTableParser>()
            .BuildServiceProvider();

        _parser = services.GetRequiredService<ITableParser>();
    }

    [Fact]
    public async Task Should_Parse_Amazon_Table_Correctly()
    {
        // Arrange
        var html = GetAmazonTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.Equal(TableStructure.SimpleKeyValue, spec.Metadata.Structure);
        Assert.True(spec.Metadata.Confidence > 0.8);
        Assert.Equal("Amazon", spec.Source.Vendor);
        Assert.Contains("Graphics Coprocessor", spec.Specifications.Keys);
        Assert.Contains("NVIDIA GeForce RTX 4060", spec.Specifications.Values);
    }

    [Fact]
    public async Task Should_Parse_VishalPeripherals_Table_Correctly()
    {
        // Arrange
        var html = GetVishalPeripheralsTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.True(spec.Metadata.Confidence > 0.7);
        Assert.Equal("VishalPeripherals", spec.Source.Vendor);
        Assert.NotEmpty(spec.Specifications);
    }

    [Fact]
    public async Task Should_Parse_PCStudio_Table_Correctly()
    {
        // Arrange
        var html = GetPCStudioTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.True(spec.Metadata.Confidence > 0.7);
        Assert.Equal("PCStudio", spec.Source.Vendor);
        Assert.NotEmpty(spec.Specifications);
    }

    [Fact]
    public async Task Should_Parse_MDComputers_Table_Correctly()
    {
        // Arrange
        var html = GetMDComputersTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.True(spec.Metadata.Confidence > 0.7);
        Assert.Equal("MDComputers", spec.Source.Vendor);
        Assert.NotEmpty(spec.Specifications);
    }

    [Fact]
    public async Task Should_Parse_PrimeABGB_Table_Correctly()
    {
        // Arrange
        var html = GetPrimeABGBTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.True(spec.Metadata.Confidence > 0.7);
        Assert.Equal("PrimeABGB", spec.Source.Vendor);
        Assert.NotEmpty(spec.Specifications);
    }

    [Fact]
    public async Task Should_Parse_Complex_Multi_Value_Table()
    {
        // Arrange
        var html = GetComplexTableHtml();
        
        // Act
        var result = await _parser.ParseAsync(html);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        
        var spec = result.Data[0];
        Assert.Equal(TableStructure.ComplexMultiValue, spec.Metadata.Structure);
        Assert.True(spec.Metadata.ContinuationRows > 0);
        Assert.Contains("Engine Clock", spec.TypedSpecifications.Keys);
    }

    [Fact]
    public async Task Should_Handle_Empty_Input_Gracefully()
    {
        // Act
        var result = await _parser.ParseAsync("");
        
        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
        Assert.Contains("Empty HTML provided", result.Warnings);
    }

    [Fact]
    public async Task Should_Cache_Results_When_Enabled()
    {
        // Arrange
        var html = GetAmazonTableHtml();
        var options = new ParsingOptions { EnableCaching = true };
        
        // Act
        var result1 = await _parser.ParseAsync(html, options);
        var result2 = await _parser.ParseAsync(html, options);
        
        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        // Second call should be faster due to caching
        Assert.True(result2.ProcessingTime < result1.ProcessingTime);
    }



    // Do not change the html, it is used for testing
    private string GetAmazonTableHtml() => "<table class=\"a-keyvalue prodDetTable\" role=\"presentation\">       <tbody><tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Graphics Coprocessor </th>  <td class=\"a-size-base prodDetAttrValue\"> NVIDIA GeForce RTX 4060 </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Graphics Card Ram </th>  <td class=\"a-size-base prodDetAttrValue\"> 8 GB </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> GPU Clock Speed </th>  <td class=\"a-size-base prodDetAttrValue\"> 2505 </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Video Output Interface </th>  <td class=\"a-size-base prodDetAttrValue\"> DisplayPort, HDMI </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Graphics Ram Type </th>  <td class=\"a-size-base prodDetAttrValue\"> GDDR6 </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Compatible Devices </th>  <td class=\"a-size-base prodDetAttrValue\"> Desktop </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Graphics Card Interface </th>  <td class=\"a-size-base prodDetAttrValue\"> PCI-Express x16 </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Memory Clock Speed </th>  <td class=\"a-size-base prodDetAttrValue\"> 17000 MHz </td> </tr>          <tr>   <th class=\"a-color-secondary a-size-base prodDetSectionEntry\"> Number of Fans </th>  <td class=\"a-size-base prodDetAttrValue\"> 3 </td> </tr>     </tbody></table>";

    private string GetVishalPeripheralsTableHtml() => "<table style=\"width: 100.061%;\"> <thead> <tr> <th style=\"width: 23.3599%;\"><strong>Category</strong></th> <th style=\"width: 76.3335%;\"><strong>Specification</strong></th> </tr> </thead> <tbody> <tr> <td style=\"width: 23.3599%;\"><strong>Graphic Engine</strong></td> <td style=\"width: 76.3335%;\">NVIDIA® GeForce RTX™ 5090</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>AI Performance</strong></td> <td style=\"width: 76.3335%;\">3593 TOPs</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Bus Standard</strong></td> <td style=\"width: 76.3335%;\">PCI Express 5.0</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>OpenGL Version</strong></td> <td style=\"width: 76.3335%;\">OpenGL® 4.6</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Video Memory</strong></td> <td style=\"width: 76.3335%;\">32GB GDDR7</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Engine Clock</strong></td> <td style=\"width: 76.3335%;\">- OC mode: 2610 MHz</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- Default mode: 2580 MHz (Boost clock)</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>CUDA Cores</strong></td> <td style=\"width: 76.3335%;\">21,760 Units</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Memory Speed</strong></td> <td style=\"width: 76.3335%;\">28 Gbps</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Memory Interface</strong></td> <td style=\"width: 76.3335%;\">512-bit</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Digital Max Resolution</strong></td> <td style=\"width: 76.3335%;\">7680 x 4320</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Interface</strong></td> <td style=\"width: 76.3335%;\">- HDMI 2.1b x 2</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- DisplayPort 2.1b x 3</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>HDCP Support</strong></td> <td style=\"width: 76.3335%;\">Yes (2.3)</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Maximum Display Support</strong></td> <td style=\"width: 76.3335%;\">4</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>NVLink / Crossfire Support</strong></td> <td style=\"width: 76.3335%;\">No</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Recommended PSU</strong></td> <td style=\"width: 76.3335%;\">1000W</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Power Connectors</strong></td> <td style=\"width: 76.3335%;\">1 x 16-pin</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Slot</strong></td> <td style=\"width: 76.3335%;\">3.8 Slot</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>AURA SYNC</strong></td> <td style=\"width: 76.3335%;\">ARGB</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Accessories</strong></td> <td style=\"width: 76.3335%;\">- 1 x Speedsetup Manual</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x ROG Graphics Card Holder</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x ROG Velcro Hook &amp; Loop</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x ROG Magnet</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x ROG Graphics Card Keycap</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x ROG PCB Ruler</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x Thank You Card</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 1 x Adapter Cable (1 to 4)</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Software</strong></td> <td style=\"width: 76.3335%;\">- ASUS GPU Tweak III, MuseTree</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- GeForce Game Ready Driver &amp; Studio Driver</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- (Download from ASUS support site)</td> </tr> <tr> <td style=\"width: 23.3599%;\"><strong>Dimensions</strong></td> <td style=\"width: 76.3335%;\">- 357.6 x 149.3 x 76 mm</td> </tr> <tr> <td style=\"width: 23.3599%;\"></td> <td style=\"width: 76.3335%;\">- 14.1 x 5.9 x 3 inches</td> </tr> </tbody> </table>";

    private string GetPCStudioTableHtml() => "<table width=\"100%\">\n<thead>\n<tr>\n<td colspan=\"2\" width=\"100%\">Specification</td>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td width=\"17%\">Graphics Engine</td>\n<td width=\"82%\">AMD Radeon RX 9060 XT</td>\n</tr>\n<tr>\n<td width=\"17%\">Bus Standard</td>\n<td width=\"82%\">PCI® Express 5.0 x16</td>\n</tr>\n<tr>\n<td width=\"17%\">DirectX</td>\n<td width=\"82%\">12 Ultimate</td>\n</tr>\n<tr>\n<td width=\"17%\">OpenGL</td>\n<td width=\"82%\">&nbsp;4.6</td>\n</tr>\n<tr>\n<td width=\"17%\">Memory</td>\n<td width=\"82%\">GDDR6 16GB</td>\n</tr>\n<tr>\n<td width=\"17%\">Engine Clock</td>\n<td width=\"82%\">Boost Clock: 3290 MHz<p></p>\n<p>Game Clock: 2700 MHz</p></td>\n</tr>\n<tr>\n<td width=\"17%\">Stream Processors</td>\n<td width=\"82%\">2048</td>\n</tr>\n<tr>\n<td width=\"17%\">Compute Units</td>\n<td width=\"82%\">32</td>\n</tr>\n<tr>\n<td width=\"17%\">Memory Clock</td>\n<td width=\"82%\">20 Gbps</td>\n</tr>\n<tr>\n<td width=\"17%\">Memory Interface</td>\n<td width=\"82%\">128-bit</td>\n</tr>\n<tr>\n<td width=\"17%\">Resolution</td>\n<td width=\"82%\">Digital Max Resolution: 7680×4320</td>\n</tr>\n<tr>\n<td width=\"17%\">Interface</td>\n<td width=\"82%\">1 x HDMI™ 2.1b<p></p>\n<p>2 x DisplayPort™ 2.1a</p></td>\n</tr>\n<tr>\n<td width=\"17%\">HDCP</td>\n<td width=\"82%\">Yes</td>\n</tr>\n<tr>\n<td width=\"17%\">Multi-view</td>\n<td width=\"82%\">3</td>\n</tr>\n<tr>\n<td width=\"17%\">Recommended PSU</td>\n<td width=\"82%\">550W</td>\n</tr>\n<tr>\n<td width=\"17%\">Power Connector</td>\n<td width=\"82%\">1x 8-pin</td>\n</tr>\n<tr>\n<td width=\"17%\">Accessories</td>\n<td width=\"82%\">1 x Quick Installation Guide</td>\n</tr>\n<tr>\n<td width=\"17%\">Dimensions</td>\n<td width=\"82%\">249 x 132 x 41 mm</td>\n</tr>\n<tr>\n<td width=\"17%\">Net Weight</td>\n<td width=\"82%\">645 g</td>\n</tr>\n<tr>\n<td width=\"17%\">Warranty</td>\n<td width=\"82%\">2 years</td>\n</tr>\n<tr>\n<td width=\"17%\">Note*</td>\n<td width=\"82%\">Price, feature and specifications are subject to change without notice</td>\n</tr>\n</tbody>\n</table>";

    private string GetMDComputersTableHtml() => "<table class=\"table\">\n                                       \n                                      <thead>\n                                          <tr>\n                                              <td colspan=\"2\">\n                                                  <strong>GRAPHICS</strong>\n                                              </td>\n                                          </tr>\n                                      </thead>\n                                      <tbody>\n                                           \n                                          <tr>\n                                              <td>Model</td>\n                                              <td>GV-N4060EAGLE-OC-8GD</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>Chipset</td>\n                                              <td>NVIDIA GEFORCE</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>GPU</td>\n                                              <td>RTX 4060</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>Engine Clock</td>\n                                              <td>2505 MHz (Reference card: 2460 MHz)</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>PCI EXPRESS</td>\n                                              <td>4.0</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>MEMORY CLOCK</td>\n                                              <td>17 Gbps</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>MEMORY SIZE</td>\n                                              <td>8 GB</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>MEMORY INTERFACE</td>\n                                              <td>128-BIT</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>MEMORY TYPE</td>\n                                              <td>GDDR6</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>OpenGL</td>\n                                              <td>4.6</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>PORTS</td>\n                                              <td>HDMI,DisplayPort</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>DirectX</td>\n                                              <td>12 Ultimate</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>RESOLUTION</td>\n                                              <td>7680 x 4320</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>COOLER</td>\n                                              <td>Triple Fan</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>Max Display Support</td>\n                                              <td>4</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>Recommended PSU</td>\n                                              <td>550W</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>GPU CORE (CUDA CORE)</td>\n                                              <td>3072</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>POWER CONNECTORS</td>\n                                              <td>1 x 8-pin</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>WARRANTY</td>\n                                              <td>3 Years</td>\n                                          </tr>\n                                           \n                                          <tr>\n                                              <td>NOTE**</td>\n                                              <td><p><span style=\"color: #ff0000;\"> Features, Price and Specifications are subject to change without notice. Images used for Representation Purpose Only.</span></p></td>\n                                          </tr>\n                                           \n                                      </tbody>\n                                                                        </table>";

    private string GetPrimeABGBTableHtml() => "<table width=\"306\">\n<tbody>\n<tr>\n<td width=\"113\">Graphics Engine</td>\n<td width=\"193\">AMD Radeon™ RX 9060 XT</td>\n</tr>\n<tr>\n<td>Bus Standard</td>\n<td>PCI® Express5.0 x16</td>\n</tr>\n<tr>\n<td>DirectX</td>\n<td>12 Ultimate</td>\n</tr>\n<tr>\n<td>OpenGL</td>\n<td>4.6</td>\n</tr>\n<tr>\n<td>Memory</td>\n<td>GDDR6 16GB</td>\n</tr>\n<tr>\n<td>Engine Clock</td>\n<td>Boost Clock: 3290 MHz</td>\n</tr>\n<tr>\n<td></td>\n<td>Game Clock: 2700 MHz</td>\n</tr>\n<tr>\n<td>Stream Processors</td>\n<td>2048</td>\n</tr>\n<tr>\n<td>Compute Units</td>\n<td>32</td>\n</tr>\n<tr>\n<td>Memory Clock</td>\n<td>20Gbps</td>\n</tr>\n<tr>\n<td>Memory Interface</td>\n<td>128-bit</td>\n</tr>\n<tr>\n<td>Resolution</td>\n<td>Digital Max Resolution: 7680×4320</td>\n</tr>\n<tr>\n<td>Interface</td>\n<td>1 x HDMI™ 2.1b</td>\n</tr>\n<tr>\n<td></td>\n<td>2 x DisplayPort™ 2.1a</td>\n</tr>\n<tr>\n<td>HDCP</td>\n<td>Yes</td>\n</tr>\n<tr>\n<td>Multi-view</td>\n<td>3</td>\n</tr>\n<tr>\n<td>Recommended PSU</td>\n<td>550W</td>\n</tr>\n<tr>\n<td>Power Connector</td>\n<td>1 x 8-pin</td>\n</tr>\n<tr>\n<td>Accessories</td>\n<td>1 x Quick Installation Guide</td>\n</tr>\n<tr>\n<td>Dimensions</td>\n<td>249 x 132 x 41 mm</td>\n</tr>\n<tr>\n<td>Net Weight</td>\n<td>645 g</td>\n</tr>\n</tbody>\n</table>";

    private string GetComplexTableHtml() => @"
        <table>
        <tr><td><strong>Engine Clock</strong></td><td>- OC mode: 2610 MHz</td></tr>
        <tr><td></td><td>- Default mode: 2580 MHz</td></tr>
        </table>";
}
